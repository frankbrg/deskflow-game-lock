@echo off
setlocal
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%~dp0Install.ps1"
if errorlevel 1 (
  echo Installation failed. Please read the message above.
  pause
  exit /b 1
)
echo Installation complete. Look for the tray icon near the Windows clock.
pause
