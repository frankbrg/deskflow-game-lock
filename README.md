# Deskflow Game Lock

**Keep Deskflow on your gaming PC while the game confines the pointer. Allow switching again when the game releases it.**

[Download for Windows](https://github.com/frankbrg/deskflow-game-lock/releases/latest) · [Report an issue](https://github.com/frankbrg/deskflow-game-lock/issues)

A small Windows companion for [Deskflow](https://github.com/deskflow/deskflow). It follows actual Windows mouse confinement instead of blocking every fullscreen application. A fullscreen video with a free pointer remains free to switch to your Mac or another Deskflow client.

> **Experimental first release.** Tested with Windows 11, Deskflow **1.27.0 in server/service mode**, and Cities: Skylines II. It is not a universal game detector. Read the single-monitor limitation before installing.

## Requirements

- Windows 10/11 **x64**, running as the Deskflow **server**.
- .NET Framework **4.8 or later**; no SDK needed for the compiled release.
- Deskflow 1.27.0 with its Windows service and **Info** logging or more verbose.
- Default log file: `%ProgramData%\Deskflow\deskflow-daemon.log`.
- Deskflow's default **Scroll Lock** cursor-lock action must be enabled and must not have been replaced with a custom lock hotkey.

Nothing is installed on the Mac/client. Other Deskflow versions and custom log paths have not been validated.

### Single Windows monitor limitation

If a game's confinement rectangle equals the entire Windows desktop, Windows reports the same rectangle as an unrestricted pointer. This version cannot distinguish them. This commonly affects fullscreen games on a PC with **one physical Windows monitor**; a Mac connected through Deskflow is not another Windows monitor.

Confinement to a smaller window or a point may still work. With multiple Windows monitors, confinement to one monitor can be detected. This conservative behavior avoids treating fullscreen video as mouse confinement.

## Install

1. Get Deskflow working between your computers first.
2. Open [Releases](https://github.com/frankbrg/deskflow-game-lock/releases/latest) and download **`deskflow-game-lock-v0.1.0-windows-x64.zip`** from **Assets**, not the source-code archives.
3. **Extract the whole ZIP**, then double-click **`Install.cmd`**.
4. Find the tray icon near the Windows clock, possibly under hidden icons.

Installs for the current user into **`%LOCALAPPDATA%\DeskflowGameLock`**, creates Start menu launch/uninstall shortcuts, and enables startup at sign-in. No administrator permissions are requested. Startup can be disabled in the tray menu.

The `.cmd` launchers run the included local PowerShell scripts. Their `ExecutionPolicy Bypass` option applies only to that PowerShell process; it does not change the saved Windows execution policy. The executable is not digitally signed in this initial release.

**Portable mode:** extract to a writable permanent folder and run **`DeskflowAutoLock.exe`** directly. No installation script is needed. If you enable startup from the tray menu, keep the executable at that path.

## Use

| Situation | Result |
| --- | --- |
| Fullscreen video or browser, pointer unrestricted | Switching stays allowed. |
| Game confines the pointer, windowed/borderless/fullscreen | Deskflow locks to the PC. |
| Pause menu releases confinement | Switching is restored automatically. |
| Alt+Tab releases the pointer | The automatic lock is removed. |
| Existing manual lock | Preserved. |

**Ctrl+Alt+F12** or double-clicking the tray icon pauses/resumes automation. Right-click the icon for options. **Scroll Lock** manually unlocks Deskflow; that override is respected until confinement is released or the foreground window changes. Only locks created by this helper are automatically removed.

Use **Run at Windows sign-in** to enable/disable automatic startup, **Quit** to exit, or **Quit and disable startup** to exit and remove startup.

### Test window

With the helper running, open PowerShell in its folder and run:

```powershell
.\DeskflowAutoLock.exe --test-window
```

**Space** toggles confinement, **F11** toggles fullscreen, and **Escape** closes the window. Fullscreen alone must not lock Deskflow. Confinement must lock it even in windowed mode; releasing confinement must unlock it without changing windows.

## Remove or update

**Installed mode:** run **Uninstall Deskflow Game Lock** from the Start menu, or `Uninstall.cmd` from the extracted release. It removes the helper, its logs, shortcuts, and startup entry. Extra files placed in the installation folder are preserved. Deskflow itself remains installed.

**Portable mode:** choose **Quit and disable startup**, then delete the extracted folder.

**Update:** extract the new release and rerun `Install.cmd`. The existing instance is asked to release its lock and exit before replacement. Installation enables Windows startup by default.

## Troubleshooting and limitations

- If nothing locks, check Deskflow's service, Info log, Scroll Lock action, and the single-monitor limitation.
- If an action has no Deskflow log acknowledgement within two seconds, automation pauses. Check the log before resuming with Ctrl+Alt+F12.
- A game may keep confinement enabled inside a menu. Pausing the simulation is not necessarily the same as releasing the pointer. Use Scroll Lock for a manual override.
- Games using recentering without `ClipCursor` may not be detected. Elevated games or games filtering synthetic input may prevent toggling. Anti-cheat compatibility is not certified.
- If the process is force-killed while locked, press Scroll Lock to release Deskflow.
- `status.txt` and `activity.log` stay next to the executable. They include state, screen coordinates, and process names. Review/redact them before posting a public issue.

**Verified:** Cities: Skylines II during gameplay and its pause menu, plus confined windowed/fullscreen test windows and fullscreen without confinement. **Not verified:** Battlefield 6 and other game engines.

## How it works

The helper reads [`GetClipCursor`](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getclipcursor) every 50 ms, compares confinement against the physical Windows virtual desktop, and checks that it matches the foreground window. It toggles Deskflow's native lock through Scroll Lock and confirms the change in Deskflow's log. Release is debounced for 150 ms.

No game-memory reading, code injection into games, screenshot capture, network requests, or modifications to game/Deskflow settings. This is mouse-confinement detection, not semantic pause-menu recognition or an allowlist of game names.

## Build

C# / Windows Forms using the .NET Framework compiler already installed on Windows. No NuGet packages or SDK downloads.

```powershell
git clone https://github.com/frankbrg/deskflow-game-lock.git
cd deskflow-game-lock
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Build.ps1 -RunTests -Package
```

Outputs: `bin/DeskflowAutoLock.exe`, a ZIP and `SHA256SUMS.txt` in `dist/`. Ten regression checks cover confined windows, negative coordinates, unrestricted fullscreen, single-monitor ambiguity, and invalid rectangles. They do not replace real-game testing.

When reporting an issue, include Windows/Deskflow versions, physical Windows monitor count, game name/display mode, and whether confinement differs between gameplay and the menu.

## License

[MIT](LICENSE). Independent project, unaffiliated with Deskflow or game publishers. Deskflow is not bundled.
