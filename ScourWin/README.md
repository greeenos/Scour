# Scour for Windows

A Windows port of [Scour](../README.md) — an app uninstaller that also hunts down the
files, registry keys, and services an app leaves behind. The original is a macOS SwiftUI app and
cannot run on Windows, so this is a fresh **C# / .NET 10 + WinUI 3** application that recreates
Scour's behavior using Windows equivalents.

## What maps to what

| Scour (macOS) | Scour for Windows |
|---|---|
| Scan `/Applications` for `.app` bundles | Read the Uninstall registry hive (HKLM 64/32-bit + HKCU) + Microsoft Store packages |
| `Info.plist` bundle id / app name / team id | `DisplayName` / `Publisher` / install location / ProductCode |
| Scan `~/Library`, `/Library`, Caches, Preferences… | Scan `%AppData%`, `%LocalAppData%`, `%ProgramData%`, `Program Files`, Start Menu, `.config` |
| `String.pearFormat()` name normalization | `Util.PearFormat()` (identical: alphanumerics only, lowercased) |
| Search sensitivity Strict / Enhanced / Deep | Same three levels in `NameMatcher` |
| Move to Trash | Send to Recycle Bin (`Microsoft.VisualBasic.FileIO`) |
| Orphaned File Search (reverse scan) | "Orphaned Files" page (`ReverseScanner`) |
| Plists/launchd/Spotlight | Registry keys + Windows services scan |

## Features in this build

- **Applications**: list every installed app, select one, scan for its leftover files / folders /
  registry keys / services, review them with sizes, and remove the selected ones (optionally
  running the app's own uninstaller first).
- **Orphaned Files**: find `AppData`/`ProgramData` folders that don't belong to any installed app.
- **Temp Files**: scan and clear Windows/user temp, Windows Update cache, thumbnail cache, crash
  dumps, etc., with per-category sizes.
- **Dev Cleanup**: reclaim space from developer caches (npm, Yarn, pnpm, pip, NuGet, Gradle, Maven,
  Cargo, Go, Composer, VS) — the analogue of Scour's Dev Environment manager.
- **RAM Cleaner**: live memory stats + frees RAM by trimming process working sets (purges the
  system standby cache too when run as admin).
- **App Updater**: lists and applies updates via winget (Windows Package Manager) — the analogue of
  the Apps Updater / Homebrew manager.
- **File Search**: search a folder tree by name and remove matches.
- **Services**: list Windows services and start/stop them (analogue of the daemon/services manager).
- **History**: a record of every removal session, with a shortcut to the Recycle Bin to restore.
- **Console**: live activity log of what the scanner/remover is doing.
- **Dashboard**: live RAM + disk gauges, installed-app count, reclaimable-space estimate, and
  quick-action shortcuts.
- **System tray**: closing the window hides Scour to the notification area (toggleable). The
  tray icon's menu offers Open, Free memory now, Check for app updates, and Exit; left-click
  restores the window.
- **Automation** (Settings):
  - **Close to tray** — keep running in the background instead of exiting.
  - **Run at sign-in** — launch automatically (starts hidden in the tray).
  - **Automatic RAM cleaning** — trims memory on an interval, above a usage threshold (runs while
    the app/tray is active).
  - **Automatic daily app updates** — registers a real **Windows Scheduled Task** that runs a
    headless `winget upgrade --all` pass daily, *even when the app is closed* (the task launches
    `Scour.exe --run-updates`).
- **Settings**: search sensitivity, theme (System/Light/Dark), Recycle Bin vs permanent delete,
  confirmation prompt, registry/service scan toggles, automation options, and path exclusions.
  Persisted to `%LocalAppData%\Scour\settings.json`. The UI is forced to en-US.

## Download & run (no build required)

A self-contained build (no .NET install needed) is produced by:

```powershell
dotnet publish -c Release -r win-x64 --self-contained true
```

The output folder `bin\Release\net10.0-windows10.0.19041.0\win-x64\publish\` contains
`Scour.exe` plus its runtime. Zip that folder, copy it anywhere, and double-click
`Scour.exe`. A ready-made `Scour-Windows.zip` is generated next to the project.

Run **as administrator** for system-wide cleanup (Program Files / ProgramData / HKLM / services).

## Build & run

```powershell
cd ScourWin
dotnet build -c Debug
dotnet run -c Debug
# or run the produced exe:
# bin\Debug\net10.0-windows10.0.19041.0\win-x64\Scour.exe
```

Requires the .NET 10 SDK. The app is configured as **unpackaged + self-contained**, so it builds
and runs from the CLI without MSIX packaging or a Visual Studio workload.

Run **as administrator** to remove leftovers under `Program Files` / `ProgramData` / HKLM. Reading
installed apps and cleaning per-user data works without elevation.

## Architecture

```
Models/        AppInfo, FileItem, SearchSensitivity
Services/      InstalledAppsService   – enumerate installed apps (registry + Store)
               LeftoverScanner        – the AppPathFinder port (matching engine)
               ReverseScanner         – orphaned-file search
               RemovalService         – Recycle Bin / registry / service removal + uninstaller
               WindowsLocations       – the set of roots to scan + protected folders
               AppIdentifiers/NameMatcher – normalized identifiers + match decision
               SettingsService, ConsoleManager
ViewModels/    AppsViewModel, OrphansViewModel  (CommunityToolkit.Mvvm)
Views/         AppsPage, OrphansPage, ConsolePage, SettingsPage
```

## Safety notes

Leftover detection is heuristic (exactly as on macOS). **Strict** sensitivity matches only exact
names; **Deep** also matches publisher and partial names and will surface more false positives.
Items go to the Recycle Bin by default so removals are reversible. Review the list — especially on
the Orphaned Files page — before removing.

## Not yet ported

The macOS-specific utilities (Homebrew manager, app Lipo/architecture stripping, `.pkg` manager,
Finder extension, development-environment manager) have no direct Windows analogue and are out of
scope for this build. Their Windows-equivalent candidates (winget integration, scheduled-task
cleanup, context-menu shell extension) are natural next steps.
