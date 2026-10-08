namespace ValheimModManager.Core.Install;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ValheimModManager.Core.Models;

public sealed class ManualModInstaller
{
    private readonly ILogger<ManualModInstaller> _logger;

    public ManualModInstaller(ILogger<ManualModInstaller>? logger = null)
    {
        _logger = logger ?? NullLogger<ManualModInstaller>.Instance;
    }

    public InstalledMod InstallFromFile(string filePath, string profileBepInExDirectory)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"File to install not found: {filePath}", filePath);
        }

        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        if (ext == ".dll")
        {
            return InstallDll(filePath, profileBepInExDirectory);
        }
        else if (ext == ".zip")
        {
            return InstallZip(filePath, profileBepInExDirectory);
        }
        else
        {
            throw new NotSupportedException($"Unsupported mod file extension '{ext}'. Only .zip and .dll files are supported.");
        }
    }

    private InstalledMod InstallDll(string filePath, string profileBepInExDirectory)
    {
        var rawName = Path.GetFileNameWithoutExtension(filePath);
        var sanitizedName = SanitizeModName(rawName);
        var targetPluginsDir = Path.Combine(profileBepInExDirectory, "plugins", sanitizedName);
        Directory.CreateDirectory(targetPluginsDir);

        var destinationDll = Path.Combine(targetPluginsDir, Path.GetFileName(filePath));
        File.Copy(filePath, destinationDll, overwrite: true);

        var version = ExtractVersionFromAssembly(filePath) ?? "1.0.0";
        var installedFiles = new List<string> { destinationDll };

        return new InstalledMod(
            Key: new ModKey("manual", sanitizedName),
            CanonicalId: new CanonicalModId("Local", sanitizedName),
            InstalledVersion: version,
            IsEnabled: true,
            InstalledAt: DateTime.UtcNow,
            InstalledFiles: installedFiles,
            Dependencies: []
        );
    }

    private InstalledMod InstallZip(string filePath, string profileBepInExDirectory)
    {
        var rawName = Path.GetFileNameWithoutExtension(filePath);
        var sanitizedName = SanitizeModName(rawName);
        var installedFiles = new List<string>();

        using var archive = ZipFile.OpenRead(filePath);

        // Check if manifest.json exists (Thunderstore package format)
        var manifestEntry = archive.GetEntry("manifest.json");
        string modName = sanitizedName;
        string modVersion = "1.0.0";

        if (manifestEntry != null)
        {
            try
            {
                using var reader = new StreamReader(manifestEntry.Open());
                var json = reader.ReadToEnd();
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("name", out var nameProp) && !string.IsNullOrWhiteSpace(nameProp.GetString()))
                {
                    modName = SanitizeModName(nameProp.GetString()!);
                }
                if (doc.RootElement.TryGetProperty("version_number", out var verProp) && !string.IsNullOrWhiteSpace(verProp.GetString()))
                {
                    modVersion = verProp.GetString()!;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse manifest.json from manual zip: {File}", filePath);
            }

            var layout = new ThunderstoreLayout();
            var extracted = layout.InstallToProfile(archive, profileBepInExDirectory, new CanonicalModId("Local", modName));
            installedFiles.AddRange(extracted);
        }
        else
        {
            // Check if archive has structured BepInEx folders
            var hasPlugins = archive.Entries.Any(e => e.FullName.StartsWith("plugins/", StringComparison.OrdinalIgnoreCase) ||
                                                      e.FullName.StartsWith("plugins\\", StringComparison.OrdinalIgnoreCase));
            var hasPatchers = archive.Entries.Any(e => e.FullName.StartsWith("patchers/", StringComparison.OrdinalIgnoreCase) ||
                                                       e.FullName.StartsWith("patchers\\", StringComparison.OrdinalIgnoreCase));
            var hasBepInEx = archive.Entries.Any(e => e.FullName.StartsWith("BepInEx/", StringComparison.OrdinalIgnoreCase) ||
                                                      e.FullName.StartsWith("BepInEx\\", StringComparison.OrdinalIgnoreCase));

            if (hasPlugins || hasPatchers || hasBepInEx)
            {
                // Structured archive
                foreach (var entry in archive.Entries)
                {
                    if (string.IsNullOrEmpty(entry.Name)) continue;

                    var entryPath = entry.FullName.Replace('\\', '/');
                    string targetPath;

                    if (entryPath.StartsWith("BepInEx/", StringComparison.OrdinalIgnoreCase))
                    {
                        var rel = entryPath["BepInEx/".Length..];
                        targetPath = Path.Combine(profileBepInExDirectory, rel.Replace('/', Path.DirectorySeparatorChar));
                    }
                    else if (entryPath.StartsWith("plugins/", StringComparison.OrdinalIgnoreCase))
                    {
                        var rel = entryPath["plugins/".Length..];
                        targetPath = Path.Combine(profileBepInExDirectory, "plugins", rel.Replace('/', Path.DirectorySeparatorChar));
                    }
                    else if (entryPath.StartsWith("patchers/", StringComparison.OrdinalIgnoreCase))
                    {
                        var rel = entryPath["patchers/".Length..];
                        targetPath = Path.Combine(profileBepInExDirectory, "patchers", rel.Replace('/', Path.DirectorySeparatorChar));
                    }
                    else if (entryPath.StartsWith("config/", StringComparison.OrdinalIgnoreCase))
                    {
                        var rel = entryPath["config/".Length..];
                        targetPath = Path.Combine(profileBepInExDirectory, "config", rel.Replace('/', Path.DirectorySeparatorChar));
                    }
                    else
                    {
                        targetPath = Path.Combine(profileBepInExDirectory, "plugins", modName, entryPath.Replace('/', Path.DirectorySeparatorChar));
                    }

                    var dir = Path.GetDirectoryName(targetPath);
                    if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

                    entry.ExtractToFile(targetPath, overwrite: true);
                    installedFiles.Add(targetPath);
                }
            }
            else
            {
                // Flat or unstructured archive (e.g. NexusMods) -> install everything into plugins/<ModName>/
                var targetPluginDir = Path.Combine(profileBepInExDirectory, "plugins", modName);
                Directory.CreateDirectory(targetPluginDir);

                foreach (var entry in archive.Entries)
                {
                    if (string.IsNullOrEmpty(entry.Name)) continue;

                    var entryPath = entry.FullName.Replace('\\', '/');
                    var targetPath = Path.Combine(targetPluginDir, entryPath.Replace('/', Path.DirectorySeparatorChar));

                    var dir = Path.GetDirectoryName(targetPath);
                    if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

                    entry.ExtractToFile(targetPath, overwrite: true);
                    installedFiles.Add(targetPath);
                }
            }

            // Attempt to determine version from an extracted dll
            var firstDll = installedFiles.FirstOrDefault(f => f.EndsWith(".dll", StringComparison.OrdinalIgnoreCase));
            if (firstDll != null)
            {
                var ver = ExtractVersionFromAssembly(firstDll);
                if (ver != null) modVersion = ver;
            }
        }

        return new InstalledMod(
            Key: new ModKey("manual", modName),
            CanonicalId: new CanonicalModId("Local", modName),
            InstalledVersion: modVersion,
            IsEnabled: true,
            InstalledAt: DateTime.UtcNow,
            InstalledFiles: installedFiles,
            Dependencies: []
        );
    }

    private static string? ExtractVersionFromAssembly(string dllPath)
    {
        try
        {
            var fvi = FileVersionInfo.GetVersionInfo(dllPath);
            if (!string.IsNullOrWhiteSpace(fvi.FileVersion))
            {
                return fvi.FileVersion.Trim();
            }
            if (!string.IsNullOrWhiteSpace(fvi.ProductVersion))
            {
                return fvi.ProductVersion.Trim();
            }
        }
        catch { }

        try
        {
            var an = AssemblyName.GetAssemblyName(dllPath);
            if (an.Version != null)
            {
                return $"{an.Version.Major}.{an.Version.Minor}.{Math.Max(0, an.Version.Build)}";
            }
        }
        catch { }

        return null;
    }

    private static string SanitizeModName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var chars = name.Select(c => invalid.Contains(c) ? '_' : c).ToArray();
        var sanitized = new string(chars).Trim();
        return string.IsNullOrWhiteSpace(sanitized) ? "ManualMod" : sanitized;
    }
}
