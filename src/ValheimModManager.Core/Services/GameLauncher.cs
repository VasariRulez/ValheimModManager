namespace ValheimModManager.Core.Services;

using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ValheimModManager.Core.Abstractions;
using ValheimModManager.Core.Models;

public sealed class GameLauncher
{
    private const string ValheimClientAppId = "892970";

    private readonly BepInExService _bepInExService;
    private readonly IProcessMonitor _processMonitor;
    private readonly ISteamLocator? _steamLocator;
    private readonly ILogger<GameLauncher> _logger;

    public GameLauncher(
        BepInExService bepInExService,
        IProcessMonitor processMonitor,
        ISteamLocator? steamLocator = null,
        ILogger<GameLauncher>? logger = null)
    {
        _bepInExService = bepInExService;
        _processMonitor = processMonitor;
        _steamLocator = steamLocator;
        _logger = logger ?? NullLogger<GameLauncher>.Instance;
    }

    public Process LaunchGame(
        GameInstall game, 
        string profileDirectory, 
        string? additionalArgs = null, 
        bool preferSteam = true)
    {
        if (!File.Exists(game.ExecutablePath))
        {
            throw new FileNotFoundException($"Game executable not found at: {game.ExecutablePath}");
        }

        if (_processMonitor.IsRunning(game.Target))
        {
            throw new InvalidOperationException($"Cannot launch game: {game.Target} is already running.");
        }

        // Configure doorstop_config.ini in game folder as primary mechanism
        _bepInExService.ConfigureDoorstopForProfile(game, profileDirectory);

        // If client and Steam launch is requested, attempt to launch via Steam (injects Steam Overlay)
        if (preferSteam && game.Target == GameTarget.Client)
        {
            var steamExe = _steamLocator?.GetSteamExecutablePath();
            if (!string.IsNullOrEmpty(steamExe) && (File.Exists(steamExe) || steamExe == "steam"))
            {
                var steamArgs = string.IsNullOrWhiteSpace(additionalArgs)
                    ? $"-applaunch {ValheimClientAppId}"
                    : $"-applaunch {ValheimClientAppId} {additionalArgs.Trim()}";

                _logger.LogInformation("Launching Valheim via Steam: {SteamExe} {Args}", steamExe, steamArgs);

                var steamStartInfo = new ProcessStartInfo
                {
                    FileName = steamExe,
                    Arguments = steamArgs,
                    WorkingDirectory = game.GameDirectory,
                    UseShellExecute = true
                };

                try
                {
                    var steamProcess = Process.Start(steamStartInfo);
                    if (steamProcess != null)
                    {
                        return steamProcess;
                    }

                    _logger.LogWarning("Process.Start returned null for Steam; falling back to direct executable.");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to launch game via Steam; falling back to direct executable.");
                }
            }
            else
            {
                _logger.LogInformation("Steam executable not found; falling back to direct executable launch.");
            }
        }

        // Direct executable launch (fallback, user preference or Dedicated Server)
        var preloaderPath = Path.Combine(profileDirectory, "BepInEx", "core", "BepInEx.Preloader.dll");
        var doorstopArgs = $"--doorstop-enabled true --doorstop-target-assembly \"{preloaderPath}\"";

        var startArgs = string.IsNullOrWhiteSpace(additionalArgs)
            ? doorstopArgs
            : $"{doorstopArgs} {additionalArgs.Trim()}";

        _logger.LogInformation("Launching game directly: {Exe} with arguments: {Args}", game.ExecutablePath, startArgs);

        var startInfo = new ProcessStartInfo
        {
            FileName = game.ExecutablePath,
            Arguments = startArgs,
            WorkingDirectory = game.GameDirectory,
            UseShellExecute = true
        };

        var process = Process.Start(startInfo);
        if (process == null)
        {
            throw new InvalidOperationException("Failed to start game process.");
        }

        return process;
    }
}
