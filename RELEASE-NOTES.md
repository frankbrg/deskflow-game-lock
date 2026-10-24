# Deskflow Game Lock 0.1.0

First public release.

## Installation

Download `deskflow-game-lock-v0.1.0-windows-x64.zip`, extract the complete archive, and run `Install.cmd`. For portable use, run `DeskflowAutoLock.exe` directly. See the [README](https://github.com/frankbrg/deskflow-game-lock#readme) for requirements, shortcuts, and removal instructions.

## Included

- Windows x64 executable; .NET Framework 4.8 required.
- Per-user installer/uninstaller and portable mode.
- Automatic Deskflow lock based on Windows mouse confinement, not fullscreen.
- Ctrl+Alt+F12 pause/resume, tray menu, and optional startup at sign-in.
- English interface and documentation, MIT license, and SHA-256 checksum.

## Compatibility

Tested with Windows 11, Deskflow **1.27.0 server/service mode**, and **Cities: Skylines II** gameplay/pause menu. Battlefield 6 is not verified.

**Single-monitor limitation:** confinement equal to the entire Windows desktop cannot be distinguished from a free pointer. A Mac connected via Deskflow does not count as a second physical Windows monitor. See the README before installing.

Experimental release. The executable is unsigned. Deskflow is not bundled. No code injection into games or network requests by the helper.
