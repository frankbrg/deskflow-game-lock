[CmdletBinding(SupportsShouldProcess = $true)]
param()

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$targetDirectory = [IO.Path]::GetFullPath((Join-Path $env:LOCALAPPDATA 'DeskflowGameLock'))
$marker = Join-Path $targetDirectory '.deskflow-game-lock-install'
if (-not (Test-Path -LiteralPath $marker)) { throw 'No managed installation was found. For portable mode, use the tray menu: Quit and disable startup, then delete the extracted folder.' }
if ((Get-Content -LiteralPath $marker -Raw) -ne 'Deskflow Game Lock') { throw 'Unexpected installation marker. No files were changed.' }
if (-not $PSCmdlet.ShouldProcess($targetDirectory, 'Remove Deskflow Game Lock, its local logs and startup entry')) { return }

$exe = Join-Path $targetDirectory 'DeskflowAutoLock.exe'
if (Test-Path -LiteralPath $exe) { Start-Process -FilePath $exe -ArgumentList '--disable' -WindowStyle Hidden -Wait }
try {
    $instance = [Threading.Mutex]::OpenExisting('Local\DeskflowAutoLock.Instance')
    try {
        $acquired = $false
        try { $acquired = $instance.WaitOne(4000) } catch [Threading.AbandonedMutexException] { $acquired = $true }
        if (-not $acquired) { throw 'The helper is still running. Quit it from the tray, then retry.' }
        $instance.ReleaseMutex()
    } finally { $instance.Dispose() }
} catch [Threading.WaitHandleCannotBeOpenedException] { }
$runKey = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run'
$startup = [Microsoft.Win32.Registry]::GetValue('HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run', 'DeskflowAutoLock', $null)
if ($startup -eq ('"' + $exe + '"')) { Remove-ItemProperty -LiteralPath $runKey -Name DeskflowAutoLock }

# Delete only known files, with native PowerShell operations and checked absolute paths.
# Any extra files the user placed in the folder are preserved.
$knownFiles = @('DeskflowAutoLock.exe', 'README.md', 'LICENSE', 'VERSION', 'Install.cmd', 'Install.ps1', 'Uninstall.cmd', 'Uninstall.ps1', '.deskflow-game-lock-install', 'activity.log', 'status.txt')
foreach ($name in $knownFiles) {
    $path = [IO.Path]::GetFullPath((Join-Path $targetDirectory $name))
    if ((Split-Path -Parent $path) -ne $targetDirectory) { throw 'Unexpected uninstall path.' }
    if (Test-Path -LiteralPath $path -PathType Leaf) { Remove-Item -LiteralPath $path -Force }
}
if (@(Get-ChildItem -LiteralPath $targetDirectory -Force).Count -eq 0) { Remove-Item -LiteralPath $targetDirectory }
$shortcutDirectory = [IO.Path]::GetFullPath((Join-Path ([Environment]::GetFolderPath('Programs')) 'Deskflow Game Lock'))
foreach ($name in @('Deskflow Game Lock.lnk', 'Uninstall Deskflow Game Lock.lnk')) {
    $path = [IO.Path]::GetFullPath((Join-Path $shortcutDirectory $name))
    if ((Split-Path -Parent $path) -ne $shortcutDirectory) { throw 'Unexpected shortcut path.' }
    if (Test-Path -LiteralPath $path -PathType Leaf) { Remove-Item -LiteralPath $path }
}
if ((Test-Path -LiteralPath $shortcutDirectory) -and @(Get-ChildItem -LiteralPath $shortcutDirectory -Force).Count -eq 0) { Remove-Item -LiteralPath $shortcutDirectory }
Write-Output 'Deskflow Game Lock removed. Deskflow itself was not modified.'
