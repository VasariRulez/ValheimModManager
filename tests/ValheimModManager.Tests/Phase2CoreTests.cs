namespace ValheimModManager.Tests;

using System;
using System.Collections.Generic;
using System.IO;
using ValheimModManager.Core.Models;
using ValheimModManager.Core.Services;
using Xunit;

public class Phase2CoreTests : IDisposable
{
    private readonly string _testDir;

    public Phase2CoreTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "VMM_Tests_Phase2_" + Guid.NewGuid().ToString("N"));
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
    public async Task DependencyResolver_ResolvesChainedDependencies()
    {
        var catalog = new CatalogService([], _testDir);

        // Mod A depends on Mod B
        // Mod B depends on Mod C
        var modC = new ModSummary(
            Key: new ModKey("thunderstore", "Author-ModC"),
            CanonicalId: new CanonicalModId("Author", "ModC"),
            Name: "ModC",
            Owner: "Author",
            PackageUrl: "",
            Description: "",
            IconUrl: "",
            LatestVersionNumber: "1.0.0",
            TotalDownloads: 10,
            RatingScore: 5,
            IsPinned: false,
            IsDeprecated: false,
            DateUpdated: DateTime.UtcNow,
            Categories: [],
            Versions: [
                new ModVersion("1.0.0", "", "", "", 10, DateTime.UtcNow, [])
            ]
        );

        var modB = new ModSummary(
            Key: new ModKey("thunderstore", "Author-ModB"),
            CanonicalId: new CanonicalModId("Author", "ModB"),
            Name: "ModB",
            Owner: "Author",
            PackageUrl: "",
            Description: "",
            IconUrl: "",
            LatestVersionNumber: "1.0.0",
            TotalDownloads: 10,
            RatingScore: 5,
            IsPinned: false,
            IsDeprecated: false,
            DateUpdated: DateTime.UtcNow,
            Categories: [],
            Versions: [
                new ModVersion("1.0.0", "", "", "", 10, DateTime.UtcNow, [
                    new Dependency("Author-ModC-1.0.0", "Author", "ModC", "1.0.0")
                ])
            ]
        );

        var modA = new ModSummary(
            Key: new ModKey("thunderstore", "Author-ModA"),
            CanonicalId: new CanonicalModId("Author", "ModA"),
            Name: "ModA",
            Owner: "Author",
            PackageUrl: "",
            Description: "",
            IconUrl: "",
            LatestVersionNumber: "1.0.0",
            TotalDownloads: 10,
            RatingScore: 5,
            IsPinned: false,
            IsDeprecated: false,
            DateUpdated: DateTime.UtcNow,
            Categories: [],
            Versions: [
                new ModVersion("1.0.0", "", "", "", 10, DateTime.UtcNow, [
                    new Dependency("Author-ModB-1.0.0", "Author", "ModB", "1.0.0")
                ])
            ]
        );

        await catalog.UpsertPackageAsync("thunderstore", modA);
        await catalog.UpsertPackageAsync("thunderstore", modB);
        await catalog.UpsertPackageAsync("thunderstore", modC);

        var resolver = new DependencyResolver(catalog);
        var profile = Profile.CreateDefault();

        var resolved = resolver.ResolveDependencies(modA, "1.0.0", profile);
        Assert.Equal(2, resolved.Count);
        Assert.Contains(resolved, r => r.CanonicalId.Name == "ModB");
        Assert.Contains(resolved, r => r.CanonicalId.Name == "ModC");
    }

    [Fact]
    public async Task UpdateService_DetectsOutdatedInstalledMods()
    {
        var catalog = new CatalogService([], _testDir);

        var remoteMod = new ModSummary(
            Key: new ModKey("thunderstore", "Author-Mod1"),
            CanonicalId: new CanonicalModId("Author", "Mod1"),
            Name: "Mod1",
            Owner: "Author",
            PackageUrl: "",
            Description: "",
            IconUrl: "",
            LatestVersionNumber: "2.0.0",
            TotalDownloads: 10,
            RatingScore: 5,
            IsPinned: false,
            IsDeprecated: false,
            DateUpdated: DateTime.UtcNow,
            Categories: [],
            Versions: []
        );

        await catalog.UpsertPackageAsync("thunderstore", remoteMod);

        var profile = Profile.CreateDefault() with {
            Mods = [
                new InstalledMod(
                    Key: new ModKey("thunderstore", "Author-Mod1"),
                    CanonicalId: new CanonicalModId("Author", "Mod1"),
                    InstalledVersion: "1.5.0",
                    IsEnabled: true,
                    InstalledAt: DateTime.UtcNow,
                    InstalledFiles: [],
                    Dependencies: []
                )
            ]
        };

        var updateService = new UpdateService(catalog);
        var results = updateService.CheckUpdates(profile);

        Assert.Single(results);
        Assert.True(results[0].HasUpdate);
        Assert.Equal("2.0.0", results[0].LatestVersion);
        Assert.Equal("1.5.0", results[0].InstalledMod.InstalledVersion);
    }

    [Fact]
    public void ProfileService_ExportsAndImportsProfileWithConfigs()
    {
        var profileDir = Path.Combine(_testDir, "profiles");
        var service = new ProfileService(profileDir);

        var p = service.CreateProfile("ExportTest");
        var pDir = service.GetProfileDirectory("ExportTest");
        var cfgPath = Path.Combine(pDir, "BepInEx", "config", "test.cfg");
        Directory.CreateDirectory(Path.GetDirectoryName(cfgPath)!);
        File.WriteAllText(cfgPath, "test=123");

        var exportZip = Path.Combine(_testDir, "export.vmmprofile");
        service.ExportProfile("ExportTest", exportZip);
        Assert.True(File.Exists(exportZip));

        var imported = service.ImportProfile(exportZip, "ImportedTest");
        Assert.Equal("ImportedTest", imported.Name);
        var importedDir = service.GetProfileDirectory("ImportedTest");
        var importedCfg = Path.Combine(importedDir, "BepInEx", "config", "test.cfg");
        Assert.True(File.Exists(importedCfg));
        Assert.Equal("test=123", File.ReadAllText(importedCfg));
    }
}
