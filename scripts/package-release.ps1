param(
    [string]$Version = "dev",
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$guiProject = Join-Path $repoRoot "src\Optimizer.Windows\Optimizer.Windows.csproj"
$agentProject = Join-Path $repoRoot "src\Optimizer.Agent\Optimizer.Agent.csproj"
$publishDir = Join-Path $repoRoot "src\Optimizer.Windows\bin\$Configuration\net8.0-windows10.0.26100.0\publish\win11-x64"
$distDir = Join-Path $repoRoot "dist"
$agentStageDir = Join-Path $distDir "_agent"
$bundleToolsDir = Join-Path $publishDir "tools"
$archivePath = Join-Path $distDir "Optimizer233-Windows-$Version-win11-x64.zip"

dotnet publish $guiProject -p:PublishProfile=win11-x64-folder -c $Configuration

if (Test-Path $distDir) {
    Remove-Item -LiteralPath $distDir -Recurse -Force
}

New-Item -ItemType Directory -Path $distDir | Out-Null

dotnet publish $agentProject -c $Configuration -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=false -o $agentStageDir

if (Test-Path $bundleToolsDir) {
    Remove-Item -LiteralPath $bundleToolsDir -Recurse -Force
}

New-Item -ItemType Directory -Path $bundleToolsDir | Out-Null
Copy-Item -LiteralPath (Join-Path $agentStageDir "Optimizer233.Agent.exe") -Destination (Join-Path $bundleToolsDir "Optimizer233.Agent.exe") -Force
Compress-Archive -Path (Join-Path $publishDir '*') -DestinationPath $archivePath -Force

Write-Host "Packaged archive: $archivePath"
