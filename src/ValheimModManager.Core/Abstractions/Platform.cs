namespace ValheimModManager.Core.Abstractions;

using System.Collections.Generic;
using ValheimModManager.Core.Models;

public interface ISteamLocator
{
    IReadOnlyList<GameInstall> FindInstalls();
    string? GetSteamExecutablePath();
}

public interface IProcessMonitor
{
    bool IsRunning(GameTarget target);
}
