namespace ValheimModManager.Tests;

using System;
using System.IO;
using System.IO.Compression;
using ValheimModManager.Core.Models;
using ValheimModManager.Core.Services;
using Xunit;

public class ProfileShareServiceTests : IDisposable
{
    private readonly string _testDir;
    private readonly ProfileShareService _service;

    public ProfileShareServiceTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "VMM_Tests_Share_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDir);
        _service = new ProfileShareService();
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
        {
            Directory.Delete(_testDir, recursive: true);
        }
    }

    [Fact]
    public void GenerateShareCode_And_ParseShareCode_Roundtrip()
    {
        var profileDir = Path.Combine(_testDir, "TestProfile");
        var configDir = Path.Combine(profileDir, "BepInEx", "config");
        Directory.CreateDirectory(configDir);

        File.WriteAllText(Path.Combine(configDir, "mod1.cfg"), "[General]\nEnabled = true\n");
        File.WriteAllText(Path.Combine(configDir, "subfolder_mod2.cfg"), "[Settings]\nSpeed = 1.5\n");

        var profile = Profile.CreateDefault("TestProfile", GameTarget.Client);
        profile.Mods.Add(new InstalledMod(
            Key: new ModKey("thunderstore", "Author-ModOne"),
            CanonicalId: new CanonicalModId("Author", "ModOne"),
            InstalledVersion: "1.2.3",
            IsEnabled: true,
            InstalledAt: DateTime.UtcNow,
            InstalledFiles: [],
            Dependencies: []
        ));
        profile.Mods.Add(new InstalledMod(
            Key: new ModKey("hexium", "Author-ModTwo"),
            CanonicalId: new CanonicalModId("Author", "ModTwo"),
            InstalledVersion: "0.9.0",
            IsEnabled: false,
            InstalledAt: DateTime.UtcNow,
            InstalledFiles: [],
            Dependencies: []
        ));

        var shareCode = _service.GenerateShareCode(profile, profileDir);

        Assert.NotNull(shareCode);
        Assert.StartsWith("vmm1-", shareCode);
        Assert.True(_service.IsVmmShareCode(shareCode));

        var parsed = _service.ParseShareCode(shareCode);

        Assert.Equal("TestProfile", parsed.ProfileName);
        Assert.Equal(GameTarget.Client, parsed.Target);
        Assert.Equal(2, parsed.Mods.Count);
        Assert.Equal("Author-ModOne", parsed.Mods[0].CanonicalId);
        Assert.Equal("1.2.3", parsed.Mods[0].Version);
        Assert.True(parsed.Mods[0].IsEnabled);
        Assert.Equal("Author-ModTwo", parsed.Mods[1].CanonicalId);
        Assert.False(parsed.Mods[1].IsEnabled);

        Assert.Equal(2, parsed.ConfigFiles.Count);
        Assert.Contains("mod1.cfg", parsed.ConfigFiles.Keys);
        Assert.Equal("[General]\nEnabled = true\n", parsed.ConfigFiles["mod1.cfg"]);
        Assert.Contains("subfolder_mod2.cfg", parsed.ConfigFiles.Keys);
        Assert.Equal("[Settings]\nSpeed = 1.5\n", parsed.ConfigFiles["subfolder_mod2.cfg"]);
    }

    [Fact]
    public void IsVmmShareCode_ValidatesPrefixCorrectly()
    {
        Assert.True(_service.IsVmmShareCode("vmm1-abc123xyz"));
        Assert.True(_service.IsVmmShareCode("  vmm1-abc123xyz  "));
        Assert.False(_service.IsVmmShareCode("018f3a59-1234"));
        Assert.False(_service.IsVmmShareCode("#r2modman"));
        Assert.False(_service.IsVmmShareCode(""));
        Assert.False(_service.IsVmmShareCode("   "));
    }

    [Fact]
    public void ParseShareCode_ThrowsOnInvalidPrefix()
    {
        Assert.Throws<FormatException>(() => _service.ParseShareCode("invalid-prefix-code"));
    }

    [Fact]
    public void ExportServerPackage_CreatesValidZipWithReadmeAndFolders()
    {
        var profileDir = Path.Combine(_testDir, "ServerProfile");
        var pluginsDir = Path.Combine(profileDir, "BepInEx", "plugins", "ValheimPlus");
        var configDir = Path.Combine(profileDir, "BepInEx", "config");
        Directory.CreateDirectory(pluginsDir);
        Directory.CreateDirectory(configDir);

        File.WriteAllText(Path.Combine(pluginsDir, "ValheimPlus.dll"), "dummy binary data");
        File.WriteAllText(Path.Combine(configDir, "valheim_plus.cfg"), "[Server]\nEnforce = true");

        var destinationZip = Path.Combine(_testDir, "ServerProfile-DedicatedServer.zip");

        _service.ExportServerPackage(profileDir, destinationZip);

        Assert.True(File.Exists(destinationZip));

        using var archive = ZipFile.OpenRead(destinationZip);
        Assert.NotNull(archive.GetEntry("SERVER_README.txt"));
        Assert.NotNull(archive.GetEntry("BepInEx/plugins/ValheimPlus/ValheimPlus.dll"));
        Assert.NotNull(archive.GetEntry("BepInEx/config/valheim_plus.cfg"));

        using var reader = new StreamReader(archive.GetEntry("SERVER_README.txt")!.Open());
        var readmeText = reader.ReadToEnd();
        Assert.Contains("DEDICATED SERVER PACKAGE", readmeText);
    }
}
