namespace Loupedeck.OrcaPlugin
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;

    public sealed record OrcaWorktree(
        String WorktreeId,
        String DisplayName,
        String Repo,
        String Status,
        Boolean IsActive,
        Boolean Unread);

    public sealed class OrcaWorktreeStatusPoller : IDisposable
    {
        private static readonly TimeSpan ReachableInterval = TimeSpan.FromSeconds(4);
        // Back off while Orca is closed so a parked keypad does not keep spawning CLI processes.
        private static readonly TimeSpan UnreachableInterval = TimeSpan.FromSeconds(15);

        private readonly OrcaCli _cli;
        private readonly CancellationTokenSource _stop = new();
        private readonly SemaphoreSlim _refreshGate = new(1, 1);

        private IReadOnlyList<OrcaWorktree> _worktrees = Array.Empty<OrcaWorktree>();

        public OrcaWorktreeStatusPoller(OrcaCli cli) => this._cli = cli;

        public event Action Changed;

        public Boolean IsReachable { get; private set; }

        public IReadOnlyList<OrcaWorktree> Worktrees => this._worktrees;

        public void Start() => _ = this.RunLoopAsync();

        public async Task ActivateAsync(String worktreeId)
        {
            var selector = $"id:{worktreeId}";
            var activated = await this._cli.RunJsonAsync(new[] { "worktree", "activate", "--worktree", selector }, this._stop.Token);
            if (!activated.HasValue)
            {
                // Orca releases without `worktree activate` still reveal a worktree by switching to one of its terminals.
                await this.SwitchToFirstTerminalAsync(selector);
            }
            await this.RefreshAsync();
        }

        private async Task SwitchToFirstTerminalAsync(String selector)
        {
            var list = await this._cli.RunJsonAsync(new[] { "terminal", "list", "--worktree", selector }, this._stop.Token);
            if (!list.HasValue
                || !list.Value.TryGetProperty("terminals", out var terminals)
                || terminals.ValueKind != JsonValueKind.Array)
            {
                return;
            }

            var handle = terminals.EnumerateArray()
                .Select(terminal => GetString(terminal, "handle"))
                .FirstOrDefault(value => !String.IsNullOrEmpty(value));
            if (handle == null)
            {
                PluginLog.Warning($"Orca worktree {selector} has no terminal to switch to");
                return;
            }
            await this._cli.RunJsonAsync(new[] { "terminal", "switch", "--terminal", handle }, this._stop.Token);
        }

        // Cancel without disposing the source: a key press may still be awaiting its token.
        public void Dispose() => this._stop.Cancel();

        private async Task RunLoopAsync()
        {
            while (!this._stop.IsCancellationRequested)
            {
                try
                {
                    await this.RefreshAsync();
                    await Task.Delay(this.IsReachable ? ReachableInterval : UnreachableInterval, this._stop.Token);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    PluginLog.Error(ex, "Orca worktree status refresh failed");
                }
            }
        }

        private async Task RefreshAsync()
        {
            await this._refreshGate.WaitAsync(this._stop.Token);
            try
            {
                var result = await this._cli.RunJsonAsync(new[] { "worktree", "ps" }, this._stop.Token);
                var worktrees = result.HasValue ? ParseWorktrees(result.Value) : Array.Empty<OrcaWorktree>();
                var reachable = result.HasValue;
                if (reachable == this.IsReachable && worktrees.SequenceEqual(this._worktrees))
                {
                    return;
                }

                this._worktrees = worktrees;
                this.IsReachable = reachable;
                this.Changed?.Invoke();
            }
            finally
            {
                this._refreshGate.Release();
            }
        }

        // Read fields leniently: Orca hosts and this plugin update independently.
        private static OrcaWorktree[] ParseWorktrees(JsonElement result)
        {
            if (!result.TryGetProperty("worktrees", out var rows) || rows.ValueKind != JsonValueKind.Array)
            {
                return Array.Empty<OrcaWorktree>();
            }

            return rows.EnumerateArray()
                .Where(row => !GetBool(row, "isArchived") && !GetBool(row, "removing"))
                .Select(row => (Pinned: GetBool(row, "isPinned"), Worktree: new OrcaWorktree(
                    GetString(row, "worktreeId") ?? "",
                    GetString(row, "displayName") ?? GetString(row, "branch") ?? "",
                    GetString(row, "repo") ?? "",
                    // sidebarStatus carries failed/interrupted/unconfirmed; older hosts only send status.
                    GetString(row, "sidebarStatus") ?? GetString(row, "status") ?? "inactive",
                    GetBool(row, "isActive"),
                    GetBool(row, "unread"))))
                .Where(entry => entry.Worktree.WorktreeId.Length > 0)
                // Stable sort keeps the host's order within pinned and unpinned groups.
                .OrderBy(entry => entry.Pinned ? 0 : 1)
                .Select(entry => entry.Worktree)
                .ToArray();
        }

        private static String GetString(JsonElement row, String name) =>
            row.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

        private static Boolean GetBool(JsonElement row, String name) =>
            row.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.True;
    }
}
