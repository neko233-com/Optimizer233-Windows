param(
    [string]$Version = "dev",
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repoRoot "src\Optimizer.Windows\Optimizer.Windows.csproj"
$publishDir = Join-Path $repoRoot "src\Optimizer.Windows\bin\$Configuration\net10.0-windows10.0.26100.0\publish\win11-x64"
$distDir = Join-Path $repoRoot "dist"
$archivePath = Join-Path $distDir "Optimizer233-Windows-$Version-win11-x64.zip"

dotnet publish $project -p:PublishProfile=win11-x64-folder -c $Configuration

if (Test-Path $distDir) {
    Remove-Item -LiteralPath $distDir -Recurse -Force
}

New-Item -ItemType Directory -Path $distDir | Out-Null
Compress-Archive -Path (Join-Path $publishDir '*') -DestinationPath $archivePath -Force

Write-Host "Packaged archive: $archivePath"
