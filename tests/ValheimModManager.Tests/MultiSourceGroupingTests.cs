namespace ValheimModManager.Tests;

using System;
using System.IO;
using ValheimModManager.Core.Models;
using ValheimModManager.Core.Services;
using Xunit;

public class MultiSourceGroupingTests : IDisposable
{
    private readonly string _testDir;

    public MultiSourceGroupingTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "VMM_Tests_Grouping_" + Guid.NewGuid().ToString("N"));
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
    public async Task SearchGrouped_CorrectlyIdentifiesNewestSourceAndGroupsPackages()
    {
        var catalog = new CatalogService([], _testDir);

        // Same canonical mod: Author-EpicLoot
        // Thunderstore has older version: 1.2.0
        var tsMod = new ModSummary(
            Key: new ModKey("thunderstore", "Author-EpicLoot"),
            CanonicalId: new CanonicalModId("Author", "EpicLoot"),
            Name: "EpicLoot",
            Owner: "Author",
            PackageUrl: "https://thunderstore.io/epicloot",
            Description: "Adds loot to Valheim",
            IconUrl: "",
            LatestVersionNumber: "1.2.0",
            TotalDownloads: 50000,
            RatingScore: 100,
            IsPinned: false,
            IsDeprecated: false,
            DateUpdated: DateTime.UtcNow.AddDays(-10),
            Categories: ["Items"],
            Versions: [
                new ModVersion("1.2.0", "Old version", "", "", 1000, DateTime.UtcNow.AddDays(-10), [])
            ]
        );

        // Hexium has newer version: 1.3.1
        var hexMod = new ModSummary(
            Key: new ModKey("hexium", "Author-EpicLoot"),
            CanonicalId: new CanonicalModId("Author", "EpicLoot"),
            Name: "EpicLoot",
            Owner: "Author",
            PackageUrl: "https://valheim.hexium.gg/epicloot",
            Description: "Adds loot to Valheim",
            IconUrl: "",
            LatestVersionNumber: "1.3.1",
            TotalDownloads: 2000,
            RatingScore: 50,
            IsPinned: false,
            IsDeprecated: false,
            DateUpdated: DateTime.UtcNow,
            Categories: ["Items"],
            Versions: [
                new ModVersion("1.3.1", "Newer version", "", "", 1100, DateTime.UtcNow, [])
            ]
        );

        await catalog.UpsertPackageAsync("thunderstore", tsMod);
        await catalog.UpsertPackageAsync("hexium", hexMod);

        // 1. Grouped search across all sources
        var allResults = catalog.SearchGrouped(new ModQuery());
        Assert.Single(allResults);

        var grouped = allResults[0];
        Assert.Equal("EpicLoot", grouped.Name);
        Assert.Equal("1.3.1", grouped.HighestVersionOverall);
        Assert.Equal(2, grouped.AvailableSources.Count);

        var newestSource = grouped.AvailableSources[0];
        Assert.Equal("hexium", newestSource.ProviderId);
        Assert.Equal("1.3.1", newestSource.LatestVersion);
        Assert.True(newestSource.IsNewestOverall);

        var olderSource = grouped.AvailableSources[1];
        Assert.Equal("thunderstore", olderSource.ProviderId);
        Assert.Equal("1.2.0", olderSource.LatestVersion);
        Assert.False(olderSource.IsNewestOverall);

        // 2. Filter specifically by Thunderstore
        var tsOnly = catalog.SearchGrouped(new ModQuery(ProviderId: "thunderstore"));
        Assert.Single(tsOnly);
        Assert.Single(tsOnly[0].AvailableSources);
        Assert.Equal("thunderstore", tsOnly[0].AvailableSources[0].ProviderId);

        // 3. Filter specifically by Hexium
        var hexOnly = catalog.SearchGrouped(new ModQuery(ProviderId: "hexium"));
        Assert.Single(hexOnly);
        Assert.Single(hexOnly[0].AvailableSources);
        Assert.Equal("hexium", hexOnly[0].AvailableSources[0].ProviderId);
    }
}
