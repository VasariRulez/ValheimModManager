namespace ValheimModManager.Core.Models;

using System;
using System.Collections.Generic;

public enum GameTarget
{
    Client,
    DedicatedServer
}

public sealed record GameInstall(
    GameTarget Target,
    string GameDirectory,
    string ExecutablePath,
    string Version = ""
);

public sealed record InstalledMod(
    ModKey Key,
    CanonicalModId CanonicalId,
    string InstalledVersion,
    bool IsEnabled,
    DateTime InstalledAt,
    IReadOnlyList<string> InstalledFiles,
    IReadOnlyList<string> Dependencies
);

public sealed record Profile(
    string Name,
    GameTarget Target,
    DateTime CreatedAt,
    DateTime LastModified,
    List<InstalledMod> Mods
)
{
    public static Profile CreateDefault(string name = "Default", GameTarget target = GameTarget.Client) =>
        new(name, target, DateTime.UtcNow, DateTime.UtcNow, []);
}
