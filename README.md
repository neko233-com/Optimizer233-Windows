# Optimizer Windows

WinUI 3 desktop utility for Windows optimization and hardware inspection.

## Stack

- WinUI 3
- Windows App SDK 2.2.0
- .NET 10
- C#

## Current features

- System overview dashboard
- Hardware inventory for CPU, GPU, memory, storage, and network
- Safe optimization shortcuts for Storage Sense, Startup Apps, Task Manager, Disk Cleanup, Windows Update, and Power Settings
- Temp folder size snapshot

## Project structure

- `src/Optimizer.Windows`: main desktop app

## Notes

- Windows only
- Current release avoids destructive one-click cleanup
- Next step can add startup impact scoring, deeper service diagnostics, and exportable reports
