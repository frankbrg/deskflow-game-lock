[CmdletBinding(SupportsShouldProcess = $true)]
param([switch]$NoStartup, [switch]$NoLaunch)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$sourceExe = Join-Path $PSScriptRoot 'DeskflowAutoLock.exe'
if (-not (Test-Path -LiteralPath $sourceExe)) { throw 'Extract the complete release ZIP before running Install.cmd.' }
if (-not [Environment]::Is64BitOperatingSystem) { throw 'Windows x64 is required.' }
$runtime = Get-ItemProperty -LiteralPath 'HKLM:\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full' -ErrorAction SilentlyContinue
if ($null -eq $runtime -or $runtime.Release -lt 528040) { throw '.NET Framework 4.8 or newer is required.' }
$targetDirectory = [IO.Path]::GetFullPath((Join-Path $env:LOCALAPPDATA 'DeskflowGameLock'))
$targetExe = Join-Path $targetDirectory 'DeskflowAutoLock.exe'
$shortcutDirectory = Join-Path ([Environment]::GetFolderPath('Programs')) 'Deskflow Game Lock'
$files = @('DeskflowAutoLock.exe', 'README.md', 'README.en.md', 'LICENSE', 'VERSION', 'Install.cmd', 'Install.ps1', 'Uninstall.cmd', 'Uninstall.ps1')
foreach ($file in $files) {
    if (-not (Test-Path -LiteralPath (Join-Path $PSScriptRoot $file))) { throw "Missing release file: $file" }
}
if (-not $PSCmdlet.ShouldProcess($targetDirectory, 'Install Deskflow Game Lock for the current user')) { return }

# Ask the existing instance to release only the lock it owns and exit.
try {
    $stopEvent = [Threading.EventWaitHandle]::OpenExisting('Local\DeskflowAutoLock.Stop')
    try { [void]$stopEvent.Set() } finally { $stopEvent.Dispose() }
} catch [Threading.WaitHandleCannotBeOpenedException] { }
try {
    $instance = [Threading.Mutex]::OpenExisting('Local\DeskflowAutoLock.Instance')
    try {
        $acquired = $false
        try { $acquired = $instance.WaitOne(4000) } catch [Threading.AbandonedMutexException] { $acquired = $true }
        if (-not $acquired) { throw 'The running helper did not exit. Quit it from its tray icon, then retry.' }
        $instance.ReleaseMutex()
    } finally { $instance.Dispose() }
} catch [Threading.WaitHandleCannotBeOpenedException] { }

New-Item -ItemType Directory -Path $targetDirectory -Force | Out-Null
foreach ($file in $files) {
    $source = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot $file))
    $target = [IO.Path]::GetFullPath((Join-Path $targetDirectory $file))
    if ($source -ne $target) { Copy-Item -LiteralPath $source -Destination $target -Force }
}
[IO.File]::WriteAllText((Join-Path $targetDirectory '.deskflow-game-lock-install'), 'Deskflow Game Lock')
$runKey = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run'
if (-not (Test-Path -LiteralPath $runKey)) { New-Item -Path $runKey -Force | Out-Null }
if ($NoStartup) { Remove-ItemProperty -LiteralPath $runKey -Name DeskflowAutoLock -ErrorAction SilentlyContinue }
else { New-ItemProperty -LiteralPath $runKey -Name DeskflowAutoLock -Value ('"' + $targetExe + '"') -PropertyType String -Force | Out-Null }

New-Item -ItemType Directory -Path $shortcutDirectory -Force | Out-Null
$shell = New-Object -ComObject WScript.Shell
$startLink = $shell.CreateShortcut((Join-Path $shortcutDirectory 'Deskflow Game Lock.lnk'))
$startLink.TargetPath = $targetExe
$startLink.WorkingDirectory = $targetDirectory
$startLink.Description = 'Follow mouse confinement to prevent unwanted Deskflow switching'
$startLink.Save()
$removeLink = $shell.CreateShortcut((Join-Path $shortcutDirectory 'Uninstall Deskflow Game Lock.lnk'))
$removeLink.TargetPath = Join-Path $targetDirectory 'Uninstall.cmd'
$removeLink.WorkingDirectory = $targetDirectory
$removeLink.Save()
if (-not $NoLaunch) { Start-Process -FilePath $targetExe -WindowStyle Hidden }
Write-Output "Installed to $targetDirectory"
Write-Output 'Startup can be changed from the tray menu. Ctrl+Alt+F12 pauses/resumes protection.'
