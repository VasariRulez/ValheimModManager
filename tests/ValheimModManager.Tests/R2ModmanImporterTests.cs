namespace ValheimModManager.Tests;

using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;
using ValheimModManager.Core.Import;
using ValheimModManager.Core.Models;
using ValheimModManager.Core.Services;
using Xunit;

public class R2ModmanImporterTests : IDisposable
{
    private readonly string _testDir;

    public R2ModmanImporterTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "VMM_Tests_R2_" + Guid.NewGuid().ToString("N"));
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
    public async Task R2ModmanImporter_ImportsR2zFileAccurately()
    {
        var profileDir = Path.Combine(_testDir, "profiles");
        var profileService = new ProfileService(profileDir);
        var catalogService = new CatalogService([], _testDir);

        // Add dummy mod to catalog to resolve
        var dummy = new ModSummary(
            Key: new ModKey("thunderstore", "ValheimModding-Jotunn"),
            CanonicalId: new CanonicalModId("ValheimModding", "Jotunn"),
            Name: "Jotunn",
            Owner: "ValheimModding",
            PackageUrl: "",
            Description: "",
            IconUrl: "",
            LatestVersionNumber: "2.20.0",
            TotalDownloads: 100,
            RatingScore: 10,
            IsPinned: false,
            IsDeprecated: false,
            DateUpdated: DateTime.UtcNow,
            Categories: [],
            Versions: []
        );
        await catalogService.UpsertPackageAsync("thunderstore", dummy);

        // Create sample r2z file
        var r2zPath = Path.Combine(_testDir, "sample_export.r2z");
        using (var archive = ZipFile.Open(r2zPath, ZipArchiveMode.Create))
        {
            var yaml = @"
profileName: CoOpPack
mods:
  - name: ValheimModding-Jotunn
    version:
      major: 2
      minor: 20
      patch: 0
    enabled: true
  - name: UnknownAuthor-UnknownMod
    version:
      major: 1
      minor: 0
      patch: 0
    enabled: true
";
            var r2xEntry = archive.CreateEntry("export.r2x");
            using (var w = new StreamWriter(r2xEntry.Open())) w.Write(yaml);

            var cfgEntry = archive.CreateEntry("BepInEx/config/jotunn.cfg");
            using (var w = new StreamWriter(cfgEntry.Open())) w.Write("LogLevel=Info");
        }

        var importer = new R2ModmanImporter(new HttpClient(), profileService, catalogService);
        var result = await importer.ImportFromR2zAsync(r2zPath);

        Assert.Equal("CoOpPack", result.CreatedProfile.Name);
        Assert.Equal(2, result.DeclaredMods.Count);
        Assert.Single(result.ResolvedCatalogMods);
        Assert.Equal("Jotunn", result.ResolvedCatalogMods[0].Name);
        Assert.Single(result.UnresolvedModNames);
        Assert.Equal("UnknownAuthor-UnknownMod", result.UnresolvedModNames[0]);

        // Verify config was extracted
        var targetCfg = Path.Combine(profileService.GetProfileDirectory("CoOpPack"), "BepInEx", "config", "jotunn.cfg");
        Assert.True(File.Exists(targetCfg));
        Assert.Equal("LogLevel=Info", File.ReadAllText(targetCfg));
    }
}
