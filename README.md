# Optimizer233-Windows

ROG dark styled WinUI 3 desktop utility for Windows 11 optimization and hardware inspection.

## Links

- Docs: `https://neko233-com.github.io/Optimizer233-Windows/`
- Releases: `https://github.com/neko233-com/Optimizer233-Windows/releases`
- Repo: `https://github.com/neko233-com/Optimizer233-Windows`

## Stack

- WinUI 3
- Windows App SDK 2.2.0
- .NET 10
- C#
- GitHub Actions
- GitHub Pages

## Current features

- ROG dark system overview dashboard
- Hardware inventory for CPU, GPU, memory, storage, and network
- Safe optimization deck with presets, checklist, and native-tool launch flow
- Temp folder size snapshot
- `.ps1` one-click installer from latest GitHub Release
- GitHub Pages docs site

## Local commands

```powershell
dotnet build src/Optimizer.Windows/Optimizer.Windows.csproj -c Release
powershell -ExecutionPolicy Bypass -File .\scripts\package-release.ps1 -Version local
```

## One-click install

```powershell
powershell -ExecutionPolicy Bypass -Command "irm https://raw.githubusercontent.com/neko233-com/Optimizer233-Windows/main/scripts/install-latest.ps1 | iex"
```

## One-click uninstall

```powershell
powershell -ExecutionPolicy Bypass -File "$env:LOCALAPPDATA\Programs\Optimizer233-Windows\uninstall.ps1"
```
