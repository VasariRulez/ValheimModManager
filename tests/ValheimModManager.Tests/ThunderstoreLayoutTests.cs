namespace ValheimModManager.Tests;

using System;
using System.IO;
using System.IO.Compression;
using ValheimModManager.Core.Install;
using ValheimModManager.Core.Models;
using Xunit;

public class ThunderstoreLayoutTests : IDisposable
{
    private readonly string _testDir;

    public ThunderstoreLayoutTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "VMM_Tests_TSLayout_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
        {
            Directory.Delete(_testDir, recursive: true);
        }
    }

    [Fact]
    public void ThunderstoreLayout_ExtractsModsAccuratelyIntoProfile()
    {
        var zipPath = Path.Combine(_testDir, "sample_mod.zip");
        using (var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
        {
            var manifest = archive.CreateEntry("manifest.json");
            using (var w = new StreamWriter(manifest.Open())) w.Write("{}");

            var readme = archive.CreateEntry("README.md");
            using (var w = new StreamWriter(readme.Open())) w.Write("Docs");

            var dll = archive.CreateEntry("MyMod.dll");
            using (var w = new StreamWriter(dll.Open())) w.Write("binary content");

            var cfg = archive.CreateEntry("config/mymod.cfg");
            using (var w = new StreamWriter(cfg.Open())) w.Write("key=value");
        }

        var profileBepDir = Path.Combine(_testDir, "Profile1", "BepInEx");
        var layout = new ThunderstoreLayout();
        var modId = new CanonicalModId("TestAuthor", "MyMod");

        using (var archive = ZipFile.OpenRead(zipPath))
        {
            Assert.True(layout.Matches(archive));
            var installed = layout.InstallToProfile(archive, profileBepDir, modId);

            var expectedDll = Path.Combine(profileBepDir, "plugins", "TestAuthor-MyMod", "MyMod.dll");
            var expectedCfg = Path.Combine(profileBepDir, "config", "mymod.cfg");

            Assert.True(File.Exists(expectedDll));
            Assert.True(File.Exists(expectedCfg));
            Assert.Contains(expectedDll, installed);
            Assert.Contains(expectedCfg, installed);

            // manifest and readme should not be extracted to plugins
            var notExpectedManifest = Path.Combine(profileBepDir, "plugins", "TestAuthor-MyMod", "manifest.json");
            Assert.False(File.Exists(notExpectedManifest));
        }
    }
}
