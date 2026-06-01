# Changelog

All notable changes to Scour for Windows are documented here. The format is based on
[Keep a Changelog](https://keepachangelog.com/), and this project aims to follow
[Semantic Versioning](https://semver.org/).

## [1.0.0] — 2026-06-01

First public release of Scour for Windows — a free app cleaner for Windows 10/11,
ported from [Pearcleaner](https://github.com/alienator88/Pearcleaner) (macOS).

### Added

- **App Uninstaller** — lists installed apps, scans for leftover files, folders,
  registry keys, and services, and removes what you choose (runs the app's own
  uninstaller first when one exists).
- **Orphaned Files** — finds `AppData` / `ProgramData` folders left behind by
  already-removed apps.
- **Temp Cleaner** — clears Windows/user temp, Windows Update cache, thumbnail
  cache, crash dumps, and more, with per-category sizes.
- **Dev Cleanup** — reclaims space from developer caches (npm, Yarn, pnpm, pip,
  NuGet, Gradle, Maven, Cargo, Go, Composer, Visual Studio).
- **RAM Cleaner** — live memory stats and one-click RAM freeing.
- **App Updater** — lists and applies app updates through winget.
- **File Search** — searches a folder tree by name and removes matches.
- **Services** — views Windows services and starts/stops them.
- **Dashboard** — live RAM and disk gauges, installed-app count, and
  reclaimable-space estimate.
- **System tray** — close-to-tray with Open / Free memory / Check for updates / Exit.
- **Automation** — close to tray, run at sign-in, automatic RAM cleaning, and
  automatic daily app updates via a Windows Scheduled Task.
- Removed items go to the **Recycle Bin** by default; every session is recorded in
  the History page.

### Notes

- Self-contained build — no .NET runtime install required.
- The build is not code-signed, so Windows SmartScreen may prompt on first launch
  ("More info" → "Run anyway").

[1.0.0]: https://github.com/greeenos/Scour/releases/tag/v1.0.0
