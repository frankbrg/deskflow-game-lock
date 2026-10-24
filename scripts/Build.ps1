[CmdletBinding()]
param([switch]$RunTests, [switch]$Package)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repoRoot = Split-Path -Parent $PSScriptRoot
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) { throw 'Windows x64 with .NET Framework 4.8 is required.' }
$version = (Get-Content -LiteralPath (Join-Path $repoRoot 'VERSION') -Raw).Trim()
if ($version -notmatch '^\d+\.\d+\.\d+$') { throw 'Invalid VERSION file.' }
$bin = Join-Path $repoRoot 'bin'
New-Item -ItemType Directory -Path $bin -Force | Out-Null
$source = Join-Path $repoRoot 'src\DeskflowAutoLock.cs'
$assemblyInfo = Join-Path $repoRoot 'src\AssemblyInfo.cs'
$manifest = Join-Path $repoRoot 'src\app.manifest'
$exe = Join-Path $bin 'DeskflowAutoLock.exe'
$common = @('/nologo', '/platform:x64', '/optimize+', '/codepage:65001', '/reference:System.Windows.Forms.dll', '/reference:System.Drawing.dll')
& $compiler @common /target:winexe "/win32manifest:$manifest" "/out:$exe" $source $assemblyInfo
if ($LASTEXITCODE -ne 0) { throw 'Application compilation failed.' }
Write-Output "Built $exe"
if ($RunTests) {
    $testExe = Join-Path $bin 'DeskflowGameLock.Tests.exe'
    & $compiler @common /target:exe /main:ConfinementTests "/out:$testExe" $source (Join-Path $repoRoot 'tests\ConfinementTests.cs')
    if ($LASTEXITCODE -ne 0) { throw 'Test compilation failed.' }
    & $testExe
    if ($LASTEXITCODE -ne 0) { throw 'Confinement regression checks failed.' }
}
if ($Package) {
    $dist = Join-Path $repoRoot 'dist'
    $packageRoot = Join-Path $dist "deskflow-game-lock-v$version-windows-x64"
    New-Item -ItemType Directory -Path $packageRoot -Force | Out-Null
    Copy-Item -LiteralPath $exe -Destination $packageRoot -Force
    foreach ($name in @('README.md', 'LICENSE', 'VERSION')) {
        Copy-Item -LiteralPath (Join-Path $repoRoot $name) -Destination $packageRoot -Force
    }
    foreach ($name in @('Install.cmd', 'Install.ps1', 'Uninstall.cmd', 'Uninstall.ps1')) {
        Copy-Item -LiteralPath (Join-Path $repoRoot "install\$name") -Destination $packageRoot -Force
    }
    # Explicit allowlist: never include logs, local settings, screenshots, or test binaries.
    $names = @('DeskflowAutoLock.exe', 'README.md', 'LICENSE', 'VERSION', 'Install.cmd', 'Install.ps1', 'Uninstall.cmd', 'Uninstall.ps1')
    $archive = Join-Path $dist "deskflow-game-lock-v$version-windows-x64.zip"
    Compress-Archive -LiteralPath @($names | ForEach-Object { Join-Path $packageRoot $_ }) -DestinationPath $archive -Force
    $hash = (Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash.ToLowerInvariant()
    [IO.File]::WriteAllText((Join-Path $dist 'SHA256SUMS.txt'), "$hash  $([IO.Path]::GetFileName($archive))`n", [Text.UTF8Encoding]::new($false))
    Write-Output "Packaged $archive"
}
