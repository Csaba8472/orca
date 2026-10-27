namespace Loupedeck.OrcaPlugin
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;

    // Drives Orca through its public CLI so local, SSH and paired-runtime worktrees all route the same way.
    public class OrcaCli
    {
        private static readonly TimeSpan CommandTimeout = TimeSpan.FromSeconds(10);

        private String _executablePath;

        public async Task<JsonElement?> RunJsonAsync(IReadOnlyList<String> args, CancellationToken cancellationToken)
        {
            var executable = this.ResolveExecutable();
            if (executable == null)
            {
                return null;
            }

            var startInfo = new ProcessStartInfo(executable)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            foreach (var arg in args)
            {
                startInfo.ArgumentList.Add(arg);
            }
            startInfo.ArgumentList.Add("--json");

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                return null;
            }

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(CommandTimeout);
            try
            {
                var stdoutTask = process.StandardOutput.ReadToEndAsync(timeout.Token);
                var stderrTask = process.StandardError.ReadToEndAsync(timeout.Token);
                await process.WaitForExitAsync(timeout.Token);
                var stdout = await stdoutTask;
                var stderr = await stderrTask;
                if (String.IsNullOrWhiteSpace(stdout))
                {
                    PluginLog.Warning($"orca {String.Join(" ", args)} exited {process.ExitCode}: {stderr.Trim()}");
                    return null;
                }

                using var document = JsonDocument.Parse(stdout);
                if (process.ExitCode == 0 && document.RootElement.TryGetProperty("result", out var result))
                {
                    return result.Clone();
                }
                var message = document.RootElement.TryGetProperty("error", out var error) && error.TryGetProperty("message", out var text)
                    ? text.GetString()
                    : stderr.Trim();
                PluginLog.Warning($"orca {String.Join(" ", args)} failed: {message}");
                return null;
            }
            catch (OperationCanceledException)
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }
                return null;
            }
            catch (JsonException ex)
            {
                PluginLog.Warning(ex, "Orca CLI printed invalid JSON");
                return null;
            }
        }

        private String ResolveExecutable()
        {
            if (this._executablePath != null && File.Exists(this._executablePath))
            {
                return this._executablePath;
            }

            foreach (var candidate in ExecutableCandidates())
            {
                if (File.Exists(candidate))
                {
                    this._executablePath = candidate;
                    return candidate;
                }
            }

            PluginLog.Warning("Orca CLI not found; install it from Orca Settings or set ORCA_CLI_PATH");
            return null;
        }

        // Logi Plugin Service starts without the user's shell PATH, so check the installer's known locations too.
        private static IEnumerable<String> ExecutableCandidates()
        {
            var overridePath = Environment.GetEnvironmentVariable("ORCA_CLI_PATH");
            if (!String.IsNullOrEmpty(overridePath))
            {
                yield return overridePath;
            }

            var commandName = OperatingSystem.IsWindows() ? "orca.exe" : "orca";
            foreach (var dir in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator))
            {
                if (dir.Length > 0)
                {
                    yield return Path.Combine(dir, commandName);
                }
            }

            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (OperatingSystem.IsMacOS())
            {
                yield return "/usr/local/bin/orca";
                yield return Path.Combine(home, ".local", "bin", "orca");
                yield return "/Applications/Orca.app/Contents/Resources/bin/orca";
                yield return Path.Combine(home, "Applications", "Orca.app", "Contents", "Resources", "bin", "orca");
            }
            else if (OperatingSystem.IsWindows())
            {
                var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                yield return Path.Combine(localAppData, "Programs", "Orca", "resources", "bin", "orca.exe");
            }
        }
    }
}
