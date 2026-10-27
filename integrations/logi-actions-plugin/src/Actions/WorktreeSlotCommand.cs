namespace Loupedeck.OrcaPlugin
{
    using System;
    using System.Threading.Tasks;

    // One key per sidebar position: pinned worktrees first, then Orca's own order.
    public class WorktreeSlotCommand : PluginDynamicCommand
    {
        private const Int32 SlotCount = 15;

        public WorktreeSlotCommand()
            : base()
        {
            for (var slot = 1; slot <= SlotCount; slot++)
            {
                this.AddParameter(slot.ToString(), $"Worktree {slot}", "Worktrees");
            }
        }

        private OrcaWorktreeStatusPoller Status => this.Plugin is OrcaPlugin orca ? orca.WorktreeStatus : null;

        protected override Boolean OnLoad()
        {
            if (this.Status != null)
            {
                this.Status.Changed += this.OnStatusChanged;
            }
            return true;
        }

        protected override Boolean OnUnload()
        {
            if (this.Status != null)
            {
                this.Status.Changed -= this.OnStatusChanged;
            }
            return true;
        }

        protected override void RunCommand(String actionParameter)
        {
            var status = this.Status;
            var worktree = this.WorktreeAt(actionParameter);
            if (status == null || worktree == null)
            {
                return;
            }

            _ = Task.Run(async () =>
            {
                try
                {
                    await status.ActivateAsync(worktree.WorktreeId);
                }
                catch (Exception ex)
                {
                    PluginLog.Error(ex, $"Could not activate Orca worktree {worktree.WorktreeId}");
                }
            });
        }

        protected override BitmapImage GetCommandImage(String actionParameter, PluginImageSize imageSize)
        {
            var status = this.Status;
            return WorktreeKeyRenderer.Render(
                this.WorktreeAt(actionParameter),
                status?.IsReachable ?? false,
                imageSize);
        }

        private void OnStatusChanged()
        {
            for (var slot = 1; slot <= SlotCount; slot++)
            {
                this.ActionImageChanged(slot.ToString());
            }
        }

        private OrcaWorktree WorktreeAt(String actionParameter)
        {
            var worktrees = this.Status?.Worktrees;
            if (worktrees == null || !Int32.TryParse(actionParameter, out var slot))
            {
                return null;
            }
            return slot >= 1 && slot <= worktrees.Count ? worktrees[slot - 1] : null;
        }
    }
}
