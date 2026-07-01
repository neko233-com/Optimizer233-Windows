param(
    [string]$Repository = "neko233-com/Optimizer233-Windows",
    [string]$InstallPath = "$env:LOCALAPPDATA\Programs\Optimizer233-Windows",
    [string]$PackagePath,
    [switch]$NoLaunch,
    [switch]$NoShortcuts,
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

function Write-Step {
    param([string]$Message)
    Write-Host "[Optimizer233-Windows] $Message" -ForegroundColor Red
}

function Ensure-Directory {
    param([string]$Path)
    if (-not (Test-Path -LiteralPath $Path)) {
        New-Item -ItemType Directory -Path $Path | Out-Null
    }
}

function New-AppShortcut {
    param(
        [string]$ShortcutPath,
        [string]$TargetPath,
        [string]$WorkingDirectory
    )

    $shell = New-Object -ComObject WScript.Shell
    $shortcut = $shell.CreateShortcut($ShortcutPath)
    $shortcut.TargetPath = $TargetPath
    $shortcut.WorkingDirectory = $WorkingDirectory
    $shortcut.IconLocation = "$TargetPath,0"
    $shortcut.Save()
}

function Get-LatestAsset {
    param([string]$RepositoryName)
    return @{
        name = "Optimizer233-Windows-latest-win11-x64.zip"
        browser_download_url = "https://github.com/$RepositoryName/releases/latest/download/Optimizer233-Windows-latest-win11-x64.zip"
    }
}

function Write-UninstallScript {
    param([string]$Directory)

    $script = @'
param([switch]$KeepData)
$ErrorActionPreference = "Stop"
$installDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$desktop = [Environment]::GetFolderPath("Desktop")
$startMenu = Join-Path $env:APPDATA "Microsoft\Windows\Start Menu\Programs"
$desktopShortcut = Join-Path $desktop "Optimizer233-Windows.lnk"
$startMenuShortcut = Join-Path $startMenu "Optimizer233-Windows.lnk"

if (Test-Path -LiteralPath $desktopShortcut) { Remove-Item -LiteralPath $desktopShortcut -Force }
if (Test-Path -LiteralPath $startMenuShortcut) { Remove-Item -LiteralPath $startMenuShortcut -Force }
if (-not $KeepData -and (Test-Path -LiteralPath $installDir)) { Remove-Item -LiteralPath $installDir -Recurse -Force }
Write-Host "Optimizer233-Windows removed."
'@

    Set-Content -LiteralPath (Join-Path $Directory "uninstall.ps1") -Value $script -Encoding UTF8
}

$tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("optimizer233-windows-" + [guid]::NewGuid().ToString("N"))
$zipPath = Join-Path $tempRoot "package.zip"
$extractPath = Join-Path $tempRoot "extract"

Ensure-Directory $tempRoot
Ensure-Directory $extractPath

$appExe = Join-Path $InstallPath "Optimizer233.Windows.exe"

Write-Step "Install path: $InstallPath"

if ($DryRun) {
    if ($PackagePath) {
        Write-Step "Dry run using local package: $PackagePath"
    }
    else {
        Write-Step "Dry run only. No download or file changes."
    }
    exit 0
}

if ($PackagePath) {
    Write-Step "Using local package: $PackagePath"
    if (-not (Test-Path -LiteralPath $PackagePath)) {
        throw "Package file not found: $PackagePath"
    }
    Copy-Item -LiteralPath $PackagePath -Destination $zipPath -Force
}
else {
    $asset = Get-LatestAsset -RepositoryName $Repository
    Write-Step "Resolved latest asset: $($asset.name)"
    Write-Step "Downloading package..."
    Invoke-WebRequest -Uri $asset.browser_download_url -OutFile $zipPath
}

Write-Step "Extracting package..."
Expand-Archive -LiteralPath $zipPath -DestinationPath $extractPath -Force

if (Test-Path -LiteralPath $InstallPath) {
    Write-Step "Removing previous installation..."
    Remove-Item -LiteralPath $InstallPath -Recurse -Force
}

Ensure-Directory $InstallPath
Get-ChildItem -LiteralPath $extractPath -Force | ForEach-Object {
    Copy-Item -LiteralPath $_.FullName -Destination $InstallPath -Recurse -Force
}
Write-UninstallScript -Directory $InstallPath

if (-not (Test-Path -LiteralPath $appExe)) {
    throw "Installed executable not found: $appExe"
}

if (-not $NoShortcuts) {
    Write-Step "Creating shortcuts..."
    $desktop = [Environment]::GetFolderPath("Desktop")
    $startMenu = Join-Path $env:APPDATA "Microsoft\Windows\Start Menu\Programs"
    New-AppShortcut -ShortcutPath (Join-Path $desktop "Optimizer233-Windows.lnk") -TargetPath $appExe -WorkingDirectory $InstallPath
    New-AppShortcut -ShortcutPath (Join-Path $startMenu "Optimizer233-Windows.lnk") -TargetPath $appExe -WorkingDirectory $InstallPath
}

if (-not $NoLaunch) {
    Write-Step "Launching app..."
    Start-Process -FilePath $appExe -WorkingDirectory $InstallPath
}

Remove-Item -LiteralPath $tempRoot -Recurse -Force
Write-Step "Install complete."
