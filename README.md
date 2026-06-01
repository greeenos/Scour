# Scour for Windows

A free app cleaner for Windows. Scour uninstalls apps **and** hunts down the leftover
files, folders, registry keys, and services they leave behind — then helps you reclaim
disk space and memory along the way.

![Platform](https://img.shields.io/badge/platform-Windows%2010%2F11-blue)
![.NET](https://img.shields.io/badge/.NET-10-512BD4)

## Features

- **App Uninstaller** — list every installed app, scan for its leftover files, folders,
  registry keys, and services, then remove what you choose (runs the app's own uninstaller
  first when one exists).
- **Orphaned Files** — find `AppData` / `ProgramData` folders left behind by apps you've
  already removed.
- **Temp Cleaner** — clear Windows and user temp, Windows Update cache, thumbnail cache,
  crash dumps, and more, with per-category sizes.
- **Dev Cleanup** — reclaim space from developer caches (npm, Yarn, pnpm, pip, NuGet,
  Gradle, Maven, Cargo, Go, Composer, Visual Studio).
- **RAM Cleaner** — live memory stats and one-click freeing of RAM.
- **App Updater** — list and apply app updates through winget (Windows Package Manager).
- **File Search** — search a folder tree by name and remove matches.
- **Services** — view Windows services and start/stop them.
- **Dashboard** — live RAM and disk gauges, installed-app count, and reclaimable-space estimate.
- **System tray** — closing the window hides Scour to the notification area; the tray menu
  offers Open, Free memory, Check for updates, and Exit.
- **Automation** — close to tray, run at sign-in, automatic RAM cleaning, and automatic daily
  app updates via a Windows Scheduled Task (works even when the app is closed).

Removed items go to the **Recycle Bin** by default, so cleanups are reversible. Every removal
session is recorded in the History page.

## Download

Grab the latest `Scour-Windows.zip` from the [Releases](../../releases) page, unzip it
anywhere, and double-click `Scour.exe` — no installation or .NET runtime required.

> **Tip:** run Scour **as administrator** for system-wide cleanup (Program Files,
> ProgramData, HKLM, and services). Reading installed apps and cleaning your own user data
> works fine without elevation.

## Requirements

- Windows 10 (build 19041 / 20H1) or Windows 11

## Build from source

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download).

```powershell
cd ScourWin
dotnet run -c Release
```

To produce a self-contained, single-folder build (no .NET install needed to run):

```powershell
dotnet publish -c Release -r win-x64 --self-contained true
```

See [ScourWin/README.md](ScourWin/README.md) for full developer documentation and architecture.

## Safety

Leftover detection is heuristic. **Strict** sensitivity matches only exact names; **Deep**
also matches the publisher and partial names and will surface more potential false positives.
Always review the list — especially on the Orphaned Files page — before removing anything.

## License

Scour is licensed under **Apache 2.0 with [Commons Clause](https://commonsclause.com/)**,
inherited from the original Pearcleaner project. You may use, modify, and contribute to the
source, but the Commons Clause **prohibits selling Scour or any modified version of it**.
See [LICENSE.md](LICENSE.md) for the full terms and [NOTICE.md](NOTICE.md) for attribution
and the statement of changes.

The Windows port (the C#/.NET source in this repo) is copyright © 2026 Adam Zelený, and the
original Pearcleaner code it derives from is copyright © alienator88 (Alin Lupascu). Both are
covered by the same Apache 2.0 + Commons Clause license.

## Credits

Scour is a Windows port derived from [Pearcleaner](https://github.com/alienator88/Pearcleaner)
by alienator88 (Alin Lupascu). Huge thanks to the original author and contributors — Scour
exists because Pearcleaner is such a good app on macOS and deserved a Windows counterpart.
