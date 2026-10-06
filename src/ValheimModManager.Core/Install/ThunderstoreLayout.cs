namespace ValheimModManager.Core.Install;

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using ValheimModManager.Core.Models;

public sealed class ThunderstoreLayout : IPackageLayout
{
    private static readonly HashSet<string> MetadataFiles = new(StringComparer.OrdinalIgnoreCase)
    {
        "manifest.json", "readme.md", "icon.png", "changelog.md", "license", "license.txt", "license.md"
    };

    public bool Matches(ZipArchive archive)
    {
        return archive.Entries.Any(e => e.FullName.Equals("manifest.json", StringComparison.OrdinalIgnoreCase));
    }

    public IReadOnlyList<string> InstallToProfile(ZipArchive archive, string profileBepInExDirectory, CanonicalModId modId)
    {
        var installedFiles = new List<string>();
        var modPluginDir = Path.Combine(profileBepInExDirectory, "plugins", $"{modId.Namespace}-{modId.Name}");
        Directory.CreateDirectory(modPluginDir);

        var hasPluginsFolder = archive.Entries.Any(e => e.FullName.StartsWith("plugins/", StringComparison.OrdinalIgnoreCase) ||
                                                        e.FullName.StartsWith("plugins\\", StringComparison.OrdinalIgnoreCase));
        var hasPatchersFolder = archive.Entries.Any(e => e.FullName.StartsWith("patchers/", StringComparison.OrdinalIgnoreCase) ||
                                                         e.FullName.StartsWith("patchers\\", StringComparison.OrdinalIgnoreCase));

        foreach (var entry in archive.Entries)
        {
            if (string.IsNullOrEmpty(entry.Name)) continue; // directory

            var entryPath = entry.FullName.Replace('\\', '/');

            // Skip top-level metadata documentation
            if (!entryPath.Contains('/') && MetadataFiles.Contains(entry.Name))
            {
                continue;
            }

            string targetPath;

            if (entryPath.StartsWith("patchers/", StringComparison.OrdinalIgnoreCase))
            {
                var rel = entryPath["patchers/".Length..];
                targetPath = Path.Combine(profileBepInExDirectory, "patchers", rel.Replace('/', Path.DirectorySeparatorChar));
            }
            else if (entryPath.StartsWith("config/", StringComparison.OrdinalIgnoreCase))
            {
                var rel = entryPath["config/".Length..];
                targetPath = Path.Combine(profileBepInExDirectory, "config", rel.Replace('/', Path.DirectorySeparatorChar));
            }
            else if (entryPath.StartsWith("plugins/", StringComparison.OrdinalIgnoreCase))
            {
                var rel = entryPath["plugins/".Length..];
                targetPath = Path.Combine(profileBepInExDirectory, "plugins", $"{modId.Namespace}-{modId.Name}", rel.Replace('/', Path.DirectorySeparatorChar));
            }
            else if (entryPath.StartsWith("BepInEx/", StringComparison.OrdinalIgnoreCase))
            {
                var rel = entryPath["BepInEx/".Length..];
                targetPath = Path.Combine(profileBepInExDirectory, rel.Replace('/', Path.DirectorySeparatorChar));
            }
            else
            {
                // Top-level or subfolder without plugins/ prefix -> put into plugins/<modId>/
                targetPath = Path.Combine(modPluginDir, entryPath.Replace('/', Path.DirectorySeparatorChar));
            }

            var dir = Path.GetDirectoryName(targetPath);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            entry.ExtractToFile(targetPath, overwrite: true);
            installedFiles.Add(targetPath);
        }

        return installedFiles;
    }
}
