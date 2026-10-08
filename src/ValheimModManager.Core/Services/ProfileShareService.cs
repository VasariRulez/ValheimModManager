namespace ValheimModManager.Core.Services;

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ValheimModManager.Core.Models;

public sealed record ProfileShareModEntry(
    string CanonicalId,
    string Version,
    string? ProviderId,
    bool IsEnabled
);

public sealed record ProfileShareManifest(
    int FormatVersion,
    string ProfileName,
    GameTarget Target,
    IReadOnlyList<ProfileShareModEntry> Mods,
    IReadOnlyDictionary<string, string> ConfigFiles
);

public sealed class ProfileShareService
{
    private const string ShareCodePrefix = "vmm1-";
    private readonly ILogger<ProfileShareService> _logger;

    public ProfileShareService(ILogger<ProfileShareService>? logger = null)
    {
        _logger = logger ?? NullLogger<ProfileShareService>.Instance;
    }

    public string GenerateShareCode(Profile profile, string profileDirectory)
    {
        var configFiles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var configDir = Path.Combine(profileDirectory, "BepInEx", "config");

        if (Directory.Exists(configDir))
        {
            var allFiles = Directory.GetFiles(configDir, "*.*", SearchOption.AllDirectories);
            foreach (var file in allFiles)
            {
                try
                {
                    // Only include reasonably sized config files (< 2 MB)
                    var fi = new FileInfo(file);
                    if (fi.Length > 2 * 1024 * 1024) continue;

                    var relPath = Path.GetRelativePath(configDir, file).Replace('\\', '/');
                    var content = File.ReadAllText(file, Encoding.UTF8);
                    configFiles[relPath] = content;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to read config file {File} for share code", file);
                }
            }
        }

        var mods = profile.Mods.Select(m => new ProfileShareModEntry(
            CanonicalId: m.CanonicalId.ToString(),
            Version: m.InstalledVersion,
            ProviderId: m.Key.ProviderId,
            IsEnabled: m.IsEnabled
        )).ToList();

        var manifest = new ProfileShareManifest(
            FormatVersion: 1,
            ProfileName: profile.Name,
            Target: profile.Target,
            Mods: mods,
            ConfigFiles: configFiles
        );

        var json = JsonSerializer.Serialize(manifest);
        var utf8Bytes = Encoding.UTF8.GetBytes(json);

        using var memoryStream = new MemoryStream();
        using (var deflateStream = new DeflateStream(memoryStream, CompressionLevel.Optimal, leaveOpen: true))
        {
            deflateStream.Write(utf8Bytes, 0, utf8Bytes.Length);
        }

        var compressedBytes = memoryStream.ToArray();
        var base64 = Convert.ToBase64String(compressedBytes);
        return $"{ShareCodePrefix}{base64}";
    }

    public bool IsVmmShareCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code)) return false;
        var trimmed = code.Trim();
        return trimmed.StartsWith(ShareCodePrefix, StringComparison.OrdinalIgnoreCase);
    }

    public ProfileShareManifest ParseShareCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Share code cannot be empty.", nameof(code));
        }

        var trimmed = code.Trim();
        if (!trimmed.StartsWith(ShareCodePrefix, StringComparison.OrdinalIgnoreCase))
        {
            throw new FormatException($"Invalid share code format: must start with '{ShareCodePrefix}'.");
        }

        var base64 = trimmed[ShareCodePrefix.Length..];
        var compressedBytes = Convert.FromBase64String(base64);

        using var inputStream = new MemoryStream(compressedBytes);
        using var deflateStream = new DeflateStream(inputStream, CompressionMode.Decompress);
        using var reader = new StreamReader(deflateStream, Encoding.UTF8);

        var json = reader.ReadToEnd();
        var manifest = JsonSerializer.Deserialize<ProfileShareManifest>(json);

        if (manifest == null)
        {
            throw new InvalidOperationException("Failed to deserialize profile share manifest.");
        }

        return manifest;
    }

    public void ExportServerPackage(string profileDirectory, string destinationZipPath)
    {
        if (!Directory.Exists(profileDirectory))
        {
            throw new DirectoryNotFoundException($"Profile directory not found: {profileDirectory}");
        }

        var tempDir = Path.Combine(Path.GetTempPath(), "VMM_ServerExport_" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(tempDir);
            var serverBepInExDir = Path.Combine(tempDir, "BepInEx");
            Directory.CreateDirectory(serverBepInExDir);

            // 1. Copy BepInEx/plugins
            var sourcePlugins = Path.Combine(profileDirectory, "BepInEx", "plugins");
            if (Directory.Exists(sourcePlugins))
            {
                CopyDirectory(sourcePlugins, Path.Combine(serverBepInExDir, "plugins"));
            }

            // 2. Copy BepInEx/patchers if present
            var sourcePatchers = Path.Combine(profileDirectory, "BepInEx", "patchers");
            if (Directory.Exists(sourcePatchers))
            {
                CopyDirectory(sourcePatchers, Path.Combine(serverBepInExDir, "patchers"));
            }

            // 3. Copy BepInEx/config
            var sourceConfig = Path.Combine(profileDirectory, "BepInEx", "config");
            if (Directory.Exists(sourceConfig))
            {
                CopyDirectory(sourceConfig, Path.Combine(serverBepInExDir, "config"));
            }

            // 4. Generate SERVER_README.txt
            var readmeContent = new StringBuilder()
                .AppendLine("================================================================================")
                .AppendLine("  VALHEIM MOD MANAGER - PACCHETTO SERVER DEDICATO / DEDICATED SERVER PACKAGE")
                .AppendLine("================================================================================")
                .AppendLine()
                .AppendLine("ISTRUZIONI DI INSTALLAZIONE:")
                .AppendLine("1. Assicurati che BepInEx per Valheim Dedicated Server sia già installato.")
                .AppendLine("2. Copia il contenuto della cartella 'BepInEx' inclusa in questo archivio nella")
                .AppendLine("   cartella 'BepInEx' principale del tuo server dedicato (sovrascrivendo se necessario).")
                .AppendLine("3. Avvia il server con il tuo consueto script di avvio (es. start_server.bat o .sh).")
                .AppendLine()
                .AppendLine("INSTALLATION INSTRUCTIONS (ENGLISH):")
                .AppendLine("1. Ensure BepInEx is already installed on your Valheim Dedicated Server.")
                .AppendLine("2. Copy the contents of the 'BepInEx' folder from this zip into your server's")
                .AppendLine("   root 'BepInEx' folder (overwrite if prompted).")
                .AppendLine("3. Launch the server using your standard startup script.")
                .AppendLine()
                .AppendLine($"Generato da Valheim Mod Manager il {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC.")
                .ToString();

            File.WriteAllText(Path.Combine(tempDir, "SERVER_README.txt"), readmeContent, Encoding.UTF8);

            if (File.Exists(destinationZipPath))
            {
                File.Delete(destinationZipPath);
            }

            ZipFile.CreateFromDirectory(tempDir, destinationZipPath, CompressionLevel.Optimal, includeBaseDirectory: false);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    private static void CopyDirectory(string sourceDir, string targetDir)
    {
        Directory.CreateDirectory(targetDir);

        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var fileName = Path.GetFileName(file);
            File.Copy(file, Path.Combine(targetDir, fileName), overwrite: true);
        }

        foreach (var subDir in Directory.GetDirectories(sourceDir))
        {
            var dirName = Path.GetFileName(subDir);
            CopyDirectory(subDir, Path.Combine(targetDir, dirName));
        }
    }
}
