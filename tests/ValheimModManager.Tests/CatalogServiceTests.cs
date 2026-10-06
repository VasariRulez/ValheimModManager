namespace ValheimModManager.Tests;

using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using ValheimModManager.Core.Models;
using ValheimModManager.Core.Providers.Thunderstore;
using ValheimModManager.Core.Services;
using Xunit;

public class CatalogServiceTests : IDisposable
{
    private readonly string _cacheDir;

    public CatalogServiceTests()
    {
        _cacheDir = Path.Combine(Path.GetTempPath(), "VMM_Tests_Catalog_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_cacheDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_cacheDir))
        {
            Directory.Delete(_cacheDir, recursive: true);
        }
    }

    [Fact]
    public async Task CatalogService_UpsertsAndSearchesCorrectly()
    {
        var catalog = new CatalogService([], _cacheDir);

        var mod1 = new ModSummary(
            Key: new ModKey("thunderstore", "Author1-Jotunn"),
            CanonicalId: new CanonicalModId("Author1", "Jotunn"),
            Name: "Jotunn",
            Owner: "Author1",
            PackageUrl: "https://example.com/jotunn",
            Description: "Modding library for Valheim",
            IconUrl: "",
            LatestVersionNumber: "2.20.0",
            TotalDownloads: 500000,
            RatingScore: 250,
            IsPinned: true,
            IsDeprecated: false,
            DateUpdated: DateTime.UtcNow,
            Categories: ["Library", "Tools"],
            Versions: []
        );

        var mod2 = new ModSummary(
            Key: new ModKey("hexium", "Author2-BetterArchery"),
            CanonicalId: new CanonicalModId("Author2", "BetterArchery"),
            Name: "BetterArchery",
            Owner: "Author2",
            PackageUrl: "https://example.com/archery",
            Description: "Improves bows and arrows",
            IconUrl: "",
            LatestVersionNumber: "1.5.0",
            TotalDownloads: 100000,
            RatingScore: 120,
            IsPinned: false,
            IsDeprecated: false,
            DateUpdated: DateTime.UtcNow,
            Categories: ["Combat"],
            Versions: []
        );

        await catalog.UpsertPackageAsync("thunderstore", mod1);
        await catalog.UpsertPackageAsync("hexium", mod2);

        // Search text
        var results = catalog.Search(new ModQuery(SearchText: "archery"));
        Assert.Single(results);
        Assert.Equal("BetterArchery", results[0].Name);

        // Filter category
        var libraryResults = catalog.Search(new ModQuery(Category: "Library"));
        Assert.Single(libraryResults);
        Assert.Equal("Jotunn", libraryResults[0].Name);

        // Save and reload disk cache
        await catalog.CompleteProviderRefreshAsync("thunderstore", 1);
        await catalog.CompleteProviderRefreshAsync("hexium", 1);

        var reloadedCatalog = new CatalogService([], _cacheDir);
        await reloadedCatalog.LoadFromLocalCacheAsync();

        Assert.Equal(2, reloadedCatalog.TotalPackageCount);
        Assert.NotNull(reloadedCatalog.FindByCanonicalId(new CanonicalModId("Author1", "Jotunn")));
    }

    [Fact]
    [Trait("Category", "Live")]
    public async Task HexiumProvider_CanFetchRealCatalog()
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        var provider = new ThunderstoreCompatibleProvider(ThunderstoreSourceOptions.Hexium, http);
        var catalog = new CatalogService([provider], _cacheDir);

        await provider.RefreshCatalogAsync(catalog);

        Assert.True(catalog.TotalPackageCount > 500, $"Expected >500 packages from Hexium, got {catalog.TotalPackageCount}");
        var results = catalog.Search(new ModQuery(SearchText: "Valheim"));
        Assert.NotEmpty(results);
    }
}
