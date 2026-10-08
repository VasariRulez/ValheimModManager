namespace ValheimModManager.Platform.Linux;

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ValheimModManager.Core.Abstractions;
using ValheimModManager.Core.Common;
using ValheimModManager.Core.Models;

public sealed class LinuxSteamLocator : ISteamLocator
{
    private const string ValheimAppId = "892970";
    private const string ValheimServerAppId = "896660";

    private readonly ILogger<LinuxSteamLocator> _logger;

    public LinuxSteamLocator(ILogger<LinuxSteamLocator>? logger = null)
    {
        _logger = logger ?? NullLogger<LinuxSteamLocator>.Instance;
    }

    public IReadOnlyList<GameInstall> FindInstalls()
    {
        var installs = new List<GameInstall>();
        var steamPaths = ResolveLinuxSteamPaths();

        foreach (var steamPath in steamPaths)
        {
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
                    _logger.LogWarning(ex, "Failed to parse linux libraryfolders.vdf at {Path}", libraryFoldersVdf);
                }
            }

            foreach (var library in libraryFolders)
            {
                // Check native and Proton Valheim client
                var clientGameDir = Path.Combine(library, "steamapps", "common", "Valheim");
                var nativeClientExe = Path.Combine(clientGameDir, "valheim.x86_64");
                var winClientExe = Path.Combine(clientGameDir, "valheim.exe");

                if (File.Exists(nativeClientExe))
                {
                    installs.Add(new GameInstall(GameTarget.Client, clientGameDir, nativeClientExe));
                }
                else if (File.Exists(winClientExe))
                {
                    installs.Add(new GameInstall(GameTarget.Client, clientGameDir, winClientExe));
                }

                // Check dedicated server
                var serverGameDir = Path.Combine(library, "steamapps", "common", "Valheim dedicated server");
                var nativeServerExe = Path.Combine(serverGameDir, "valheim_server.x86_64");
                var winServerExe = Path.Combine(serverGameDir, "valheim_server.exe");

                if (File.Exists(nativeServerExe))
                {
                    installs.Add(new GameInstall(GameTarget.DedicatedServer, serverGameDir, nativeServerExe));
                }
                else if (File.Exists(winServerExe))
                {
                    installs.Add(new GameInstall(GameTarget.DedicatedServer, serverGameDir, winServerExe));
                }
            }
        }

        return installs;
    }

    public string? GetSteamExecutablePath()
    {
        var standardBinaries = new[]
        {
            "/usr/bin/steam",
            "/usr/games/steam",
            "/usr/local/bin/steam"
        };

        foreach (var bin in standardBinaries)
        {
            if (File.Exists(bin)) return bin;
        }

        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (!string.IsNullOrEmpty(home))
        {
            var flatpakBin = Path.Combine(home, ".local", "share", "flatpak", "exports", "bin", "com.valvesoftware.Steam");
            if (File.Exists(flatpakBin)) return flatpakBin;
        }

        const string systemFlatpak = "/var/lib/flatpak/exports/bin/com.valvesoftware.Steam";
        if (File.Exists(systemFlatpak)) return systemFlatpak;

        // If steam folders were found, return the command "steam" (assuming PATH)
        if (ResolveLinuxSteamPaths().Count > 0)
        {
            return "steam";
        }

        return null;
    }

    private static List<string> ResolveLinuxSteamPaths()
    {
        var list = new List<string>();
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (string.IsNullOrEmpty(home)) return list;

        // 1. Standard Steam on Linux / Steam Deck
        var p1 = Path.Combine(home, ".local", "share", "Steam");
        if (Directory.Exists(p1)) list.Add(p1);

        // 2. Legacy Steam symlink
        var p2 = Path.Combine(home, ".steam", "steam");
        if (Directory.Exists(p2)) list.Add(p2);

        // 3. Flatpak Steam (very common on Steam Deck desktop mode)
        var p3 = Path.Combine(home, ".var", "app", "com.valvesoftware.Steam", ".local", "share", "Steam");
        if (Directory.Exists(p3)) list.Add(p3);

        return list;
    }
}
