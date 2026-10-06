namespace ValheimModManager.Platform.Windows;

using System;
using System.Diagnostics;
using System.Linq;
using ValheimModManager.Core.Abstractions;
using ValheimModManager.Core.Models;

public sealed class WindowsProcessMonitor : IProcessMonitor
{
    public bool IsRunning(GameTarget target)
    {
        var processName = target switch
        {
            GameTarget.Client => "valheim",
            GameTarget.DedicatedServer => "valheim_server",
            _ => "valheim"
        };

        var processes = Process.GetProcessesByName(processName);
        return processes.Length > 0;
    }
}
