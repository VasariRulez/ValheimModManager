namespace ValheimModManager.Core.Install;

using System.Collections.Generic;
using System.IO.Compression;
using ValheimModManager.Core.Models;

public interface IPackageLayout
{
    bool Matches(ZipArchive archive);
    IReadOnlyList<string> InstallToProfile(ZipArchive archive, string profileBepInExDirectory, CanonicalModId modId);
}
