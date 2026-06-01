using System.Text.Json;
using Scour.Models;

namespace Scour.Services;

/// <summary>
/// Persisted user settings. Windows analogue of @AppStorage + FolderSettingsManager.
/// Stored as JSON under %LocalAppData%\Scour\settings.json.
/// </summary>
public sealed class SettingsService
{
    private static readonly string Dir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Scour");
    private static readonly string FilePath = Path.Combine(Dir, "settings.json");

    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    public sealed class Data
    {
        public SearchSensitivity Sensitivity { get; set; } = SearchSensitivity.Strict;
        public string Theme { get; set; } = "System"; // System | Light | Dark
        public string AccentHex { get; set; } = "#34C759";
        public bool ConfirmBeforeRemoval { get; set; } = true;
        public bool SendToRecycleBin { get; set; } = true;
        public bool IncludeRegistry { get; set; } = true;
        public bool IncludeServices { get; set; } = true;
        public bool IncludeStartMenu { get; set; } = true;
        public bool ScanTextContent { get; set; }

        // ----- Automation -----
        /// <summary>Closing the window hides it to the system tray instead of exiting.</summary>
        public bool MinimizeToTray { get; set; } = true;

        /// <summary>Launch Scour automatically when the user signs in.</summary>
        public bool RunOnStartup { get; set; }

        /// <summary>Automatically trim process working sets on an interval.</summary>
        public bool AutoClearRam { get; set; }

        /// <summary>Minutes between automatic RAM trims.</summary>
        public int AutoClearRamMinutes { get; set; } = 30;

        /// <summary>Only auto-trim RAM when memory load is above this percent.</summary>
        public int AutoClearRamThreshold { get; set; } = 80;

        /// <summary>Run `winget upgrade --all` automatically once per day while the app is open.</summary>
        public bool AutoUpdateApps { get; set; }

        /// <summary>Last time the automatic update runner executed (UTC).</summary>
        public DateTime LastAutoUpdateUtc { get; set; } = DateTime.MinValue;

        /// <summary>Paths/fragments excluded from app leftover search (FolderSettingsManager.fileFolderPathsApps).</summary>
        public List<string> AppExclusions { get; set; } = new();

        /// <summary>Extra roots the user wants included in scans.</summary>
        public List<string> ExtraSearchRoots { get; set; } = new();
    }

    public Data Current { get; private set; } = new();

    public void Load()
    {
        try
        {
            if (File.Exists(FilePath))
                Current = JsonSerializer.Deserialize<Data>(File.ReadAllText(FilePath)) ?? new Data();
        }
        catch (Exception ex)
        {
            ConsoleManager.Shared.Append($"Failed to load settings: {ex.Message}");
            Current = new Data();
        }
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Dir);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(Current, JsonOpts));
        }
        catch (Exception ex)
        {
            ConsoleManager.Shared.Append($"Failed to save settings: {ex.Message}");
        }
    }
}
