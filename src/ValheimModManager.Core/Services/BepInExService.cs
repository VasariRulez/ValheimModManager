namespace ValheimModManager.Core.Services;

using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ValheimModManager.Core.Install;
using ValheimModManager.Core.Models;

public sealed record BepInExStatus(
    bool IsInstalledInProfile,
    bool IsGameConfigured,
    string? Version,
    string? TargetAssemblyConfigured
);

public sealed class BepInExService
{
    private readonly ILogger<BepInExService> _logger;

    public BepInExService(ILogger<BepInExService>? logger = null)
    {
        _logger = logger ?? NullLogger<BepInExService>.Instance;
    }

    public BepInExStatus GetStatus(GameInstall? game, string profileDirectory)
    {
        var profilePreloader = Path.Combine(profileDirectory, "BepInEx", "core", "BepInEx.Preloader.dll");
        var isInstalledInProfile = File.Exists(profilePreloader);

        var isGameConfigured = false;
        string? targetAssembly = null;
        string? version = null;

        if (game != null && Directory.Exists(game.GameDirectory))
        {
            var winhttp = Path.Combine(game.GameDirectory, "winhttp.dll");
            var doorstop = Path.Combine(game.GameDirectory, "doorstop_config.ini");
            isGameConfigured = File.Exists(winhttp) && File.Exists(doorstop);

            if (File.Exists(doorstop))
            {
                var lines = File.ReadAllLines(doorstop);
                var targetLine = lines.FirstOrDefault(l => l.Trim().StartsWith("target_assembly", StringComparison.OrdinalIgnoreCase) ||
                                                           l.Trim().StartsWith("targetAssembly", StringComparison.OrdinalIgnoreCase));
                if (targetLine != null)
                {
                    var eq = targetLine.IndexOf('=');
                    if (eq >= 0)
                    {
                        targetAssembly = targetLine[(eq + 1)..].Trim();
                    }
                }
            }

            if (isInstalledInProfile)
            {
                version = FileVersionInfo.GetVersionInfo(profilePreloader).FileVersion;
            }

            /*
            var versionFile = Path.Combine(game.GameDirectory, ".doorstop_version");
            if (version is null && File.Exists(versionFile))
            {
                version = File.ReadAllText(versionFile).Trim();
            }
            */
        }

        return new BepInExStatus(
            IsInstalledInProfile: isInstalledInProfile,
            IsGameConfigured: isGameConfigured,
            Version: version ?? (isInstalledInProfile ? "5.4.x" : null),
            TargetAssemblyConfigured: targetAssembly
        );
    }

    public void InstallToProfile(string zipPath, string profileDirectory)
    {
        _logger.LogInformation("Installing BepInEx from {Zip} to {Profile}", zipPath, profileDirectory);
        using var archive = ZipFile.OpenRead(zipPath);

        var bepSubfolder = archive.Entries.FirstOrDefault(e => e.FullName.StartsWith("BepInExPack_Valheim/", StringComparison.OrdinalIgnoreCase));
        var prefix = bepSubfolder != null ? "BepInExPack_Valheim/" : "";

        var bepInExTarget = Path.Combine(profileDirectory, "BepInEx");
        Directory.CreateDirectory(bepInExTarget);

        foreach (var entry in archive.Entries)
        {
            if (string.IsNullOrEmpty(entry.Name)) continue;

            var fullPath = entry.FullName.Replace('\\', '/');
            if (!string.IsNullOrEmpty(prefix) && fullPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                fullPath = fullPath[prefix.Length..];
            }

            // Extract BepInEx core folders into profile/BepInEx
            if (fullPath.StartsWith("BepInEx/", StringComparison.OrdinalIgnoreCase))
            {
                var rel = fullPath["BepInEx/".Length..];
                var target = Path.Combine(bepInExTarget, rel.Replace('/', Path.DirectorySeparatorChar));
                var dir = Path.GetDirectoryName(target);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                entry.ExtractToFile(target, overwrite: true);
            }
        }
    }

    public void DeployToGame(string zipPath, GameInstall game, string profileDirectory)
    {
        if (!Directory.Exists(game.GameDirectory))
        {
            throw new DirectoryNotFoundException($"Game directory does not exist: {game.GameDirectory}");
        }

        _logger.LogInformation("Deploying Doorstop hooks to game at {Dir} for profile {Profile}", game.GameDirectory, profileDirectory);
        using var archive = ZipFile.OpenRead(zipPath);

        var bepSubfolder = archive.Entries.FirstOrDefault(e => e.FullName.StartsWith("BepInExPack_Valheim/", StringComparison.OrdinalIgnoreCase));
        var prefix = bepSubfolder != null ? "BepInExPack_Valheim/" : "";

        // Extract winhttp.dll, .doorstop_version and doorstop_libs
        foreach (var entry in archive.Entries)
        {
            if (string.IsNullOrEmpty(entry.Name)) continue;

            var fullPath = entry.FullName.Replace('\\', '/');
            if (!string.IsNullOrEmpty(prefix) && fullPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                fullPath = fullPath[prefix.Length..];
            }

            if (fullPath.Equals("winhttp.dll", StringComparison.OrdinalIgnoreCase) ||
                fullPath.Equals(".doorstop_version", StringComparison.OrdinalIgnoreCase) ||
                fullPath.StartsWith("doorstop_libs/", StringComparison.OrdinalIgnoreCase))
            {
                var target = Path.Combine(game.GameDirectory, fullPath.Replace('/', Path.DirectorySeparatorChar));
                var dir = Path.GetDirectoryName(target);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                entry.ExtractToFile(target, overwrite: true);
            }
        }

        // Configure doorstop_config.ini to point directly to active profile
        ConfigureDoorstopForProfile(game, profileDirectory);
    }

    public void ConfigureDoorstopForProfile(GameInstall game, string profileDirectory)
    {
        var doorstopPath = Path.Combine(game.GameDirectory, "doorstop_config.ini");
        var preloaderPath = Path.Combine(profileDirectory, "BepInEx", "core", "BepInEx.Preloader.dll");

        var sb = new StringBuilder();
        sb.AppendLine("[General]");
        sb.AppendLine("enabled = true");
        sb.AppendLine($"target_assembly = {preloaderPath}");
        sb.AppendLine("redirect_output_log = false");
        sb.AppendLine("ignore_disable_switch = false");
        sb.AppendLine();
        sb.AppendLine("[UnityMono]");
        sb.AppendLine("dll_search_path_override =");
        sb.AppendLine("debug_enabled = false");

        File.WriteAllText(doorstopPath, sb.ToString());
        _logger.LogInformation("Configured doorstop_config.ini with target_assembly = {Preloader}", preloaderPath);
    }

    public void RestoreVanilla(GameInstall game)
    {
        if (!Directory.Exists(game.GameDirectory)) return;

        var winhttp = Path.Combine(game.GameDirectory, "winhttp.dll");
        var doorstop = Path.Combine(game.GameDirectory, "doorstop_config.ini");
        var doorstopVer = Path.Combine(game.GameDirectory, ".doorstop_version");

        if (File.Exists(winhttp)) File.Delete(winhttp);
        if (File.Exists(doorstop)) File.Delete(doorstop);
        if (File.Exists(doorstopVer)) File.Delete(doorstopVer);

        _logger.LogInformation("Restored game to vanilla state by removing doorstop and winhttp from {Dir}", game.GameDirectory);
    }
}
