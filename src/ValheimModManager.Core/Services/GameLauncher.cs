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
    private readonly BepInExService _bepInExService;
    private readonly IProcessMonitor _processMonitor;
    private readonly ILogger<GameLauncher> _logger;

    public GameLauncher(
        BepInExService bepInExService,
        IProcessMonitor processMonitor,
        ILogger<GameLauncher>? logger = null)
    {
        _bepInExService = bepInExService;
        _processMonitor = processMonitor;
        _logger = logger ?? NullLogger<GameLauncher>.Instance;
    }

    public Process LaunchGame(GameInstall game, string profileDirectory, string? additionalArgs = null)
    {
        if (!File.Exists(game.ExecutablePath))
        {
            throw new FileNotFoundException($"Game executable not found at: {game.ExecutablePath}");
        }

        if (_processMonitor.IsRunning(game.Target))
        {
            throw new InvalidOperationException($"Cannot launch game: {game.Target} is already running.");
        }

        // Configure doorstop_config.ini in game folder as primary/fallback mechanism
        _bepInExService.ConfigureDoorstopForProfile(game, profileDirectory);

        var preloaderPath = Path.Combine(profileDirectory, "BepInEx", "core", "BepInEx.Preloader.dll");
        var doorstopArgs = $"--doorstop-enabled true --doorstop-target-assembly \"{preloaderPath}\"";

        var startArgs = string.IsNullOrWhiteSpace(additionalArgs)
            ? doorstopArgs
            : $"{doorstopArgs} {additionalArgs}";

        _logger.LogInformation("Launching game: {Exe} with arguments: {Args}", game.ExecutablePath, startArgs);

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
