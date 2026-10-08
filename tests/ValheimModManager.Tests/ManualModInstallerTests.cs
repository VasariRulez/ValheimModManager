namespace ValheimModManager.Tests;

using System;
using System.IO;
using System.IO.Compression;
using ValheimModManager.Core.Install;
using Xunit;

public class ManualModInstallerTests : IDisposable
{
    private readonly string _testDir;
    private readonly ManualModInstaller _installer;

    public ManualModInstallerTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "VMM_Tests_ManualMod_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDir);
        _installer = new ManualModInstaller();
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
        {
            Directory.Delete(_testDir, recursive: true);
        }
    }

    [Fact]
    public void InstallDll_CreatesPluginFolderAndReturnsInstalledMod()
    {
        var sourceDll = Path.Combine(_testDir, "CustomWeaponMod.dll");
        File.WriteAllText(sourceDll, "dummy dll content");

        var profileBepDir = Path.Combine(_testDir, "ProfileBepInEx");
        Directory.CreateDirectory(profileBepDir);

        var mod = _installer.InstallFromFile(sourceDll, profileBepDir);

        Assert.Equal("manual", mod.Key.ProviderId);
        Assert.Equal("Local", mod.CanonicalId.Namespace);
        Assert.Equal("CustomWeaponMod", mod.CanonicalId.Name);
        Assert.True(mod.IsEnabled);
        Assert.Single(mod.InstalledFiles);

        var installedDll = Path.Combine(profileBepDir, "plugins", "CustomWeaponMod", "CustomWeaponMod.dll");
        Assert.True(File.Exists(installedDll));
        Assert.Equal(installedDll, mod.InstalledFiles[0]);
    }

    [Fact]
    public void InstallZip_WithManifest_InstallsThunderstorePackage()
    {
        var zipPath = Path.Combine(_testDir, "ThunderstorePackage.zip");
        using (var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
        {
            var manifestEntry = archive.CreateEntry("manifest.json");
            using (var writer = new StreamWriter(manifestEntry.Open()))
            {
                writer.Write("{\"name\": \"EpicLoot\", \"version_number\": \"2.1.0\"}");
            }

            var dllEntry = archive.CreateEntry("plugins/EpicLoot.dll");
            using (var writer = new StreamWriter(dllEntry.Open()))
            {
                writer.Write("dummy dll");
            }
        }

        var profileBepDir = Path.Combine(_testDir, "ProfileBepInEx");
        Directory.CreateDirectory(profileBepDir);

        var mod = _installer.InstallFromFile(zipPath, profileBepDir);

        Assert.Equal("manual", mod.Key.ProviderId);
        Assert.Equal("EpicLoot", mod.CanonicalId.Name);
        Assert.Equal("2.1.0", mod.InstalledVersion);
        Assert.NotEmpty(mod.InstalledFiles);
    }

    [Fact]
    public void InstallZip_WithPluginsFolder_InstallsStructuredArchive()
    {
        var zipPath = Path.Combine(_testDir, "StructuredMod.zip");
        using (var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
        {
            var pluginEntry = archive.CreateEntry("plugins/StructuredMod/Mod.dll");
            using (var writer = new StreamWriter(pluginEntry.Open()))
            {
                writer.Write("dummy dll");
            }

            var configEntry = archive.CreateEntry("config/StructuredMod.cfg");
            using (var writer = new StreamWriter(configEntry.Open()))
            {
                writer.Write("dummy cfg");
            }
        }

        var profileBepDir = Path.Combine(_testDir, "ProfileBepInEx");
        Directory.CreateDirectory(profileBepDir);

        var mod = _installer.InstallFromFile(zipPath, profileBepDir);

        Assert.Equal("manual", mod.Key.ProviderId);
        Assert.True(File.Exists(Path.Combine(profileBepDir, "plugins", "StructuredMod", "Mod.dll")));
        Assert.True(File.Exists(Path.Combine(profileBepDir, "config", "StructuredMod.cfg")));
    }

    [Fact]
    public void InstallZip_FlatArchive_InstallsIntoModPluginDirectory()
    {
        var zipPath = Path.Combine(_testDir, "NexusFlatMod.zip");
        using (var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
        {
            var dllEntry = archive.CreateEntry("NexusFlatMod.dll");
            using (var writer = new StreamWriter(dllEntry.Open()))
            {
                writer.Write("dummy dll");
            }

            var assetEntry = archive.CreateEntry("assets/texture.png");
            using (var writer = new StreamWriter(assetEntry.Open()))
            {
                writer.Write("dummy texture");
            }
        }

        var profileBepDir = Path.Combine(_testDir, "ProfileBepInEx");
        Directory.CreateDirectory(profileBepDir);

        var mod = _installer.InstallFromFile(zipPath, profileBepDir);

        Assert.Equal("manual", mod.Key.ProviderId);
        Assert.Equal("NexusFlatMod", mod.CanonicalId.Name);
        Assert.True(File.Exists(Path.Combine(profileBepDir, "plugins", "NexusFlatMod", "NexusFlatMod.dll")));
        Assert.True(File.Exists(Path.Combine(profileBepDir, "plugins", "NexusFlatMod", "assets", "texture.png")));
    }

    [Fact]
    public void InstallFromFile_ThrowsOnUnsupportedExtension()
    {
        var textFile = Path.Combine(_testDir, "notes.txt");
        File.WriteAllText(textFile, "unsupported");

        var profileBepDir = Path.Combine(_testDir, "ProfileBepInEx");

        Assert.Throws<NotSupportedException>(() => _installer.InstallFromFile(textFile, profileBepDir));
    }
}
