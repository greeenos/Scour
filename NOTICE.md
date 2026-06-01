# NOTICE

## Scour for Windows

Copyright © 2026 Adam Zelený.

The Windows application "Scour" (the C#/.NET source in this repository and the
distributed `Scour.exe` build) was written by Adam Zelený. As a derivative work
of Pearcleaner (see below), it remains subject to the upstream license; the
copyright in the original Pearcleaner code continues to belong to its author.

## Original work

Scour is a Windows port derived from **Pearcleaner**, copyright © alienator88
(Alin Lupascu), available at https://github.com/alienator88/Pearcleaner.

Pearcleaner is licensed under the **Apache License, Version 2.0, with the
Commons Clause** condition. Scour is distributed under the same license. See
[LICENSE.md](LICENSE.md) for the full terms.

In accordance with the Apache License, Version 2.0:

- **Section 4(b) — statement of changes.** Scour is a from-scratch
  reimplementation of Pearcleaner for Windows. It was rewritten in C#/.NET
  (WinUI) rather than Swift/SwiftUI, and its leftover-detection logic and
  name-matching algorithms are derived from and adapted from the Pearcleaner
  source (for example the `PearFormat` normalization and the
  `AppPathFinder`-style identifier matching). The project has been renamed from
  "Pearcleaner" to "Scour", and its branding, identifiers, and ownership
  metadata have been changed accordingly.
- **Section 4(c) — retained notices.** The upstream copyright and Licensor
  designation (alienator88 / Alin Lupascu) are retained here and in
  [LICENSE.md](LICENSE.md). No per-file copyright headers were carried over,
  because the Windows source files are newly authored C# rather than copies of
  the original Swift files.

The Commons Clause condition prohibits selling the Software or any modified
version of it. This condition continues to apply to Scour and to anyone who
redistributes it.
