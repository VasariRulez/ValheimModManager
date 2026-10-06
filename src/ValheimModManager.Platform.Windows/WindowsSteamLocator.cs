namespace ValheimModManager.Platform.Windows;

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Win32;
using System.Runtime.Versioning;
using ValheimModManager.Core.Abstractions;
using ValheimModManager.Core.Common;
using ValheimModManager.Core.Models;

[SupportedOSPlatform("windows")]
public sealed class WindowsSteamLocator : ISteamLocator
{
    private const string ValheimAppId = "892970";
    private const string ValheimServerAppId = "896660";

    private readonly ILogger<WindowsSteamLocator> _logger;

    public WindowsSteamLocator(ILogger<WindowsSteamLocator>? logger = null)
    {
        _logger = logger ?? NullLogger<WindowsSteamLocator>.Instance;
    }

    public IReadOnlyList<GameInstall> FindInstalls()
    {
        var installs = new List<GameInstall>();
        var steamPath = ResolveSteamPath();

        if (string.IsNullOrEmpty(steamPath) || !Directory.Exists(steamPath))
        {
            _logger.LogInformation("Steam directory not found via registry or standard paths.");
            return installs;
        }

        var libraryFolders = new List<string> { steamPath };
        var libraryFoldersVdf = Path.Combine(steamPath, "steamapps", "libraryfolders.vdf");

        if (File.Exists(libraryFoldersVdf))
        {
            try
            {
                var content = File.ReadAllText(libraryFoldersVdf);
                var parsed = VdfParser.ParseLibraryFolders(content);
                libraryFolders.AddRange(parsed);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse libraryfolders.vdf");
            }
        }

        foreach (var library in libraryFolders)
        {
            // Check for Valheim Client (App ID 892970)
            var clientManifest = Path.Combine(library, "steamapps", $"appmanifest_{ValheimAppId}.acf");
            var clientGameDir = Path.Combine(library, "steamapps", "common", "Valheim");
            var clientExe = Path.Combine(clientGameDir, "valheim.exe");

            if (File.Exists(clientExe))
            {
                _logger.LogInformation("Found Valheim client at {Path}", clientExe);
                installs.Add(new GameInstall(GameTarget.Client, clientGameDir, clientExe));
            }

            // Check for Valheim Dedicated Server (App ID 896660)
            var serverManifest = Path.Combine(library, "steamapps", $"appmanifest_{ValheimServerAppId}.acf");
            var serverGameDir = Path.Combine(library, "steamapps", "common", "Valheim dedicated server");
            var serverExe = Path.Combine(serverGameDir, "valheim_server.exe");

            if (File.Exists(serverExe))
            {
                _logger.LogInformation("Found Valheim dedicated server at {Path}", serverExe);
                installs.Add(new GameInstall(GameTarget.DedicatedServer, serverGameDir, serverExe));
            }
        }

        return installs;
    }

    private static string? ResolveSteamPath()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");
            var path = key?.GetValue("SteamPath") as string;
            if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
            {
                return path;
            }
        }
        catch
        {
            // ignore
        }

        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"Software\WOW6432Node\Valve\Steam");
            var path = key?.GetValue("InstallPath") as string;
            if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
            {
                return path;
            }
        }
        catch
        {
            // ignore
        }

        var defaultPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam");
        return Directory.Exists(defaultPath) ? defaultPath : null;
    }
}
