@echo off
setlocal
(
  powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%~dp0Uninstall.ps1"
  if errorlevel 1 (
    echo Uninstall failed. Please read the message above.
    pause
    exit /b 1
  )
  echo Deskflow Game Lock has been removed. Deskflow itself was not changed.
  pause
)
