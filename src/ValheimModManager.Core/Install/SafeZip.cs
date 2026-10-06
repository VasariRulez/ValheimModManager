namespace ValheimModManager.Core.Install;

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

public static class SafeZip
{
    public static void ExtractEntrySafe(ZipArchiveEntry entry, string destinationFullPath, bool overwrite = true)
    {
        var targetDir = Path.GetFullPath(destinationFullPath);
        var entryTarget = Path.GetFullPath(Path.Combine(targetDir, entry.FullName.Replace('/', Path.DirectorySeparatorChar)));

        if (!entryTarget.StartsWith(targetDir, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Malicious zip entry detected (Zip-Slip attempt): {entry.FullName}");
        }

        if (string.IsNullOrEmpty(entry.Name))
        {
            // Directory entry
            Directory.CreateDirectory(entryTarget);
            return;
        }

        var parentDir = Path.GetDirectoryName(entryTarget);
        if (!string.IsNullOrEmpty(parentDir))
        {
            Directory.CreateDirectory(parentDir);
        }

        entry.ExtractToFile(entryTarget, overwrite);
    }

    public static List<string> ExtractArchiveSafe(ZipArchive archive, string destinationFullPath, bool overwrite = true)
    {
        var extractedFiles = new List<string>();
        var targetDir = Path.GetFullPath(destinationFullPath);
        Directory.CreateDirectory(targetDir);

        foreach (var entry in archive.Entries)
        {
            if (string.IsNullOrEmpty(entry.Name)) continue; // skip folder entries in the files list

            var entryRelative = entry.FullName.Replace('/', Path.DirectorySeparatorChar);
            var entryTarget = Path.GetFullPath(Path.Combine(targetDir, entryRelative));

            if (!entryTarget.StartsWith(targetDir, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Malicious zip entry detected (Zip-Slip attempt): {entry.FullName}");
            }

            var parent = Path.GetDirectoryName(entryTarget);
            if (!string.IsNullOrEmpty(parent))
            {
                Directory.CreateDirectory(parent);
            }

            entry.ExtractToFile(entryTarget, overwrite);
            extractedFiles.Add(entryTarget);
        }

        return extractedFiles;
    }
}
