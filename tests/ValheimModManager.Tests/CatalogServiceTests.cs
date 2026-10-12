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

    [Fact]
    public async Task SearchGrouped_RelevancePrioritizesNameAndOwnerOverDescription()
    {
        var catalog = new CatalogService([], _cacheDir);

        var descMatchHighDownloads = new ModSummary(
            Key: new ModKey("thunderstore", "BigAuthor-MegaPack"),
            CanonicalId: new CanonicalModId("BigAuthor", "MegaPack"),
            Name: "MegaPack",
            Owner: "BigAuthor",
            PackageUrl: "",
            Description: "Compatible with EpicLoot and other mods",
            IconUrl: "",
            LatestVersionNumber: "1.0.0",
            TotalDownloads: 2_000_000,
            RatingScore: 500,
            IsPinned: false,
            IsDeprecated: false,
            DateUpdated: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Categories: [],
            Versions: []
        );

        var nameMatchLowerDownloads = new ModSummary(
            Key: new ModKey("thunderstore", "RandyKnapp-EpicLoot"),
            CanonicalId: new CanonicalModId("RandyKnapp", "EpicLoot"),
            Name: "EpicLoot",
            Owner: "RandyKnapp",
            PackageUrl: "",
            Description: "Adds magic drops to Valheim",
            IconUrl: "",
            LatestVersionNumber: "0.9.0",
            TotalDownloads: 500_000,
            RatingScore: 300,
            IsPinned: false,
            IsDeprecated: false,
            DateUpdated: new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
            Categories: [],
            Versions: []
        );

        await catalog.UpsertPackageAsync("thunderstore", descMatchHighDownloads);
        await catalog.UpsertPackageAsync("thunderstore", nameMatchLowerDownloads);

        // With Relevance sort, exact name match ("EpicLoot") comes first despite fewer downloads
        var relevanceResults = catalog.SearchGrouped(new ModQuery(
            SearchText: "EpicLoot",
            SortBy: ModSortBy.Relevance,
            SortDescending: true));

        Assert.Equal(2, relevanceResults.Count);
        Assert.Equal("EpicLoot", relevanceResults[0].Name);
        Assert.Equal("MegaPack", relevanceResults[1].Name);

        // With pure Downloads sort, MegaPack (2M) comes first
        var downloadResults = catalog.SearchGrouped(new ModQuery(
            SearchText: "EpicLoot",
            SortBy: ModSortBy.Downloads,
            SortDescending: true));

        Assert.Equal("MegaPack", downloadResults[0].Name);
        Assert.Equal("EpicLoot", downloadResults[1].Name);
    }

    [Fact]
    public async Task SearchGrouped_SortsByAllCriteriaAndDirections()
    {
        var catalog = new CatalogService([], _cacheDir);

        var modA = new ModSummary(
            Key: new ModKey("thunderstore", "Zeta-AlphaMod"),
            CanonicalId: new CanonicalModId("Zeta", "AlphaMod"),
            Name: "AlphaMod",
            Owner: "Zeta",
            PackageUrl: "",
            Description: "First alphabetically",
            IconUrl: "",
            LatestVersionNumber: "1.0.0",
            TotalDownloads: 100,
            RatingScore: 50,
            IsPinned: false,
            IsDeprecated: false,
            DateUpdated: new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc),
            Categories: [],
            Versions: []
        );

        var modB = new ModSummary(
            Key: new ModKey("thunderstore", "Beta-OmegaMod"),
            CanonicalId: new CanonicalModId("Beta", "OmegaMod"),
            Name: "OmegaMod",
            Owner: "Beta",
            PackageUrl: "",
            Description: "Last alphabetically",
            IconUrl: "",
            LatestVersionNumber: "2.0.0",
            TotalDownloads: 9000,
            RatingScore: 10,
            IsPinned: false,
            IsDeprecated: false,
            DateUpdated: new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc),
            Categories: [],
            Versions: []
        );

        await catalog.UpsertPackageAsync("thunderstore", modA);
        await catalog.UpsertPackageAsync("thunderstore", modB);

        // Name Asc (A -> Z)
        var byNameAsc = catalog.SearchGrouped(new ModQuery(SortBy: ModSortBy.Name, SortDescending: false));
        Assert.Equal("AlphaMod", byNameAsc[0].Name);

        // Name Desc (Z -> A)
        var byNameDesc = catalog.SearchGrouped(new ModQuery(SortBy: ModSortBy.Name, SortDescending: true));
        Assert.Equal("OmegaMod", byNameDesc[0].Name);

        // Author Asc (A -> Z: Beta before Zeta)
        var byAuthorAsc = catalog.SearchGrouped(new ModQuery(SortBy: ModSortBy.Author, SortDescending: false));
        Assert.Equal("Beta", byAuthorAsc[0].Owner);

        // LastUpdated Desc (2026-10-01 before 2026-01-10)
        var byDateDesc = catalog.SearchGrouped(new ModQuery(SortBy: ModSortBy.LastUpdated, SortDescending: true));
        Assert.Equal("OmegaMod", byDateDesc[0].Name);

        // Rating Desc (50 before 10)
        var byRatingDesc = catalog.SearchGrouped(new ModQuery(SortBy: ModSortBy.Rating, SortDescending: true));
        Assert.Equal("AlphaMod", byRatingDesc[0].Name);
    }
}
