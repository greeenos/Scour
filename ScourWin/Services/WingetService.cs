using System.Diagnostics;
using System.Text;
using Scour.Models;

namespace Scour.Services;

/// <summary>
/// Wraps the Windows Package Manager (winget) to list and apply app updates — the Windows
/// analogue of Scour's Apps Updater / Homebrew manager.
/// </summary>
public sealed class WingetService
{
    public bool IsAvailable => ResolveWinget() is not null;

    /// <summary>Lists apps with an available upgrade by parsing `winget upgrade` table output.</summary>
    public async Task<List<UpdatablePackage>> ListUpdatesAsync(CancellationToken ct = default)
    {
        var winget = ResolveWinget();
        if (winget is null)
        {
            ConsoleManager.Shared.Append("winget not found. Install 'App Installer' from the Microsoft Store.");
            return new();
        }

        ConsoleManager.Shared.Append("Checking for updates (winget upgrade)...");
        var output = await RunCaptureAsync(winget,
            "upgrade --include-unknown --accept-source-agreements --disable-interactivity", ct);
        var pkgs = ParseUpgradeTable(output);
        ConsoleManager.Shared.Append($"{pkgs.Count} update(s) available.");
        return pkgs;
    }

    /// <summary>Updates a single package by id, streaming progress to the console.</summary>
    public async Task<bool> UpdateAsync(string id, CancellationToken ct = default)
    {
        var winget = ResolveWinget();
        if (winget is null) return false;
        ConsoleManager.Shared.Append($"Updating {id}...");
        int code = await RunStreamAsync(winget,
            $"upgrade --id \"{id}\" --silent --accept-package-agreements --accept-source-agreements --disable-interactivity", ct);
        var ok = code == 0;
        ConsoleManager.Shared.Append(ok ? $"Updated {id}." : $"Update of {id} exited with code {code}.");
        return ok;
    }

    /// <summary>Upgrades everything in one shot (used by the headless scheduled-task runner).</summary>
    public async Task<int> UpdateAllAsync(CancellationToken ct = default)
    {
        var winget = ResolveWinget();
        if (winget is null) { ConsoleManager.Shared.Append("winget not found; cannot auto-update."); return -1; }
        ConsoleManager.Shared.Append("Running 'winget upgrade --all'...");
        int code = await RunStreamAsync(winget,
            "upgrade --all --silent --include-unknown --accept-package-agreements --accept-source-agreements --disable-interactivity", ct);
        ConsoleManager.Shared.Append($"Auto-update pass finished (exit {code}).");
        return code;
    }

    // ----- table parsing -----

    internal static List<UpdatablePackage> ParseUpgradeTable(string output)
    {
        var result = new List<UpdatablePackage>();
        var lines = output.Replace("\r", "").Split('\n');

        // Find the header row: contains "Name", "Id", "Version", "Available".
        int headerIdx = -1;
        for (int i = 0; i < lines.Length; i++)
        {
            var l = lines[i];
            if (l.Contains("Name") && l.Contains("Id") && l.Contains("Version") && l.Contains("Available"))
            {
                headerIdx = i;
                break;
            }
        }
        if (headerIdx < 0) return result;

        var header = lines[headerIdx];
        int idCol = header.IndexOf("Id", StringComparison.Ordinal);
        int verCol = header.IndexOf("Version", StringComparison.Ordinal);
        int availCol = header.IndexOf("Available", StringComparison.Ordinal);
        int srcCol = header.IndexOf("Source", StringComparison.Ordinal);
        if (idCol < 0 || verCol < 0 || availCol < 0) return result;

        for (int i = headerIdx + 1; i < lines.Length; i++)
        {
            var line = lines[i];
            if (line.Length == 0) continue;
            if (line.StartsWith("---")) continue;
            // Footer like "N upgrades available." or package-count notices.
            if (line.TrimStart().StartsWith("The following") ) continue;
            if (line.Length < availCol) continue;
            if (!char.IsLetterOrDigit(line.TrimStart().FirstOrDefault())) continue;

            string Slice(int start, int end) =>
                start >= line.Length ? "" : line[start..Math.Min(end < 0 ? line.Length : end, line.Length)].Trim();

            var name = Slice(0, idCol);
            var id = Slice(idCol, verCol);
            var ver = Slice(verCol, availCol);
            var avail = Slice(availCol, srcCol < 0 ? line.Length : srcCol);
            var src = srcCol < 0 ? "" : Slice(srcCol, line.Length);

            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(avail)) continue;
            // Skip the trailing "N package(s) have version numbers that cannot be determined" note row.
            if (id.Contains(' ')) continue;

            result.Add(new UpdatablePackage
            {
                Name = name, Id = id, CurrentVersion = ver, AvailableVersion = avail, Source = src
            });
        }
        return result;
    }

    // ----- process helpers -----

    private static string? _resolved;
    private static bool _resolvedDone;

    private static string? ResolveWinget()
    {
        if (_resolvedDone) return _resolved;
        _resolvedDone = true;
        // winget.exe lives under WindowsApps; "winget" on PATH usually works.
        foreach (var candidate in new[] { "winget.exe", "winget" })
        {
            try
            {
                var psi = new ProcessStartInfo(candidate, "--version")
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var p = Process.Start(psi);
                if (p is null) continue;
                p.WaitForExit(5000);
                _resolved = candidate;
                return _resolved;
            }
            catch { }
        }
        _resolved = null;
        return null;
    }

    private static async Task<string> RunCaptureAsync(string file, string args, CancellationToken ct)
    {
        var psi = new ProcessStartInfo(file, args)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8
        };
        using var p = new Process { StartInfo = psi };
        var sb = new StringBuilder();
        p.OutputDataReceived += (_, e) => { if (e.Data != null) sb.AppendLine(e.Data); };
        p.Start();
        p.BeginOutputReadLine();
        await p.WaitForExitAsync(ct);
        return sb.ToString();
    }

    private static async Task<int> RunStreamAsync(string file, string args, CancellationToken ct)
    {
        var psi = new ProcessStartInfo(file, args)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8
        };
        using var p = new Process { StartInfo = psi };
        p.OutputDataReceived += (_, e) => { if (!string.IsNullOrWhiteSpace(e.Data)) ConsoleManager.Shared.Append(e.Data!); };
        p.ErrorDataReceived += (_, e) => { if (!string.IsNullOrWhiteSpace(e.Data)) ConsoleManager.Shared.Append(e.Data!); };
        p.Start();
        p.BeginOutputReadLine();
        p.BeginErrorReadLine();
        await p.WaitForExitAsync(ct);
        return p.ExitCode;
    }
}
