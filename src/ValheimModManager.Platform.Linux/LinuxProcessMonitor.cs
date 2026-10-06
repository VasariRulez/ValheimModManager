namespace ValheimModManager.Platform.Linux;

using System;
using System.Diagnostics;
using System.Linq;
using ValheimModManager.Core.Abstractions;
using ValheimModManager.Core.Models;

public sealed class LinuxProcessMonitor : IProcessMonitor
{
    public bool IsRunning(GameTarget target)
    {
        var targetNames = target switch
        {
            GameTarget.Client => new[] { "valheim.x86_64", "valheim.exe", "valheim" },
            GameTarget.DedicatedServer => new[] { "valheim_server.x86_64", "valheim_server.exe", "valheim_server" },
            _ => new[] { "valheim.x86_64", "valheim.exe", "valheim" }
        };

        var all = Process.GetProcesses();
        return all.Any(p => targetNames.Any(name => p.ProcessName.Equals(name, StringComparison.OrdinalIgnoreCase)));
    }
}
