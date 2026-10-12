namespace ValheimModManager.Core.Services;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ValheimModManager.Core.Abstractions;
using ValheimModManager.Core.Models;

public sealed class CatalogService : ICatalogSink
{
    private readonly IEnumerable<IModProvider> _providers;
    private readonly string _cacheDirectory;
    private readonly ILogger<CatalogService> _logger;

    // In-memory catalog storage: ProviderId -> Key -> ModSummary
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, ModSummary>> _catalog = new(StringComparer.OrdinalIgnoreCase);

    public CatalogService(
        IEnumerable<IModProvider> providers,
        string? cacheDirectory = null,
        ILogger<CatalogService>? logger = null)
    {
        _providers = providers;
        _logger = logger ?? NullLogger<CatalogService>.Instance;
        _cacheDirectory = cacheDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ValheimModManager", "cache", "catalog");

        Directory.CreateDirectory(_cacheDirectory);
    }

    public Task UpsertPackageAsync(string providerId, ModSummary package, CancellationToken ct = default)
    {
        var providerDict = _catalog.GetOrAdd(providerId, _ => new ConcurrentDictionary<string, ModSummary>(StringComparer.OrdinalIgnoreCase));
        providerDict[package.Key.ExternalId] = package;
        return Task.CompletedTask;
    }

    public async Task CompleteProviderRefreshAsync(string providerId, int totalCount, CancellationToken ct = default)
    {
        _logger.LogInformation("Saving local catalog cache for {ProviderId} ({TotalCount} packages)", providerId, totalCount);
        if (_catalog.TryGetValue(providerId, out var dict))
        {
            var cacheFile = Path.Combine(_cacheDirectory, $"{providerId}.json");
            var tempFile = cacheFile + ".tmp";
            var list = dict.Values.ToList();

            await using (var fs = File.Create(tempFile))
            {
                await JsonSerializer.SerializeAsync(fs, list, cancellationToken: ct);
            }

            if (File.Exists(cacheFile))
            {
                File.Delete(cacheFile);
            }
            File.Move(tempFile, cacheFile);
        }
    }

    public async Task LoadFromLocalCacheAsync(CancellationToken ct = default)
    {
        if (!Directory.Exists(_cacheDirectory)) return;

        foreach (var file in Directory.GetFiles(_cacheDirectory, "*.json"))
        {
            var providerId = Path.GetFileNameWithoutExtension(file);
            try
            {
                await using var fs = File.OpenRead(file);
                var list = await JsonSerializer.DeserializeAsync<List<ModSummary>>(fs, cancellationToken: ct);
                if (list != null)
                {
                    var dict = _catalog.GetOrAdd(providerId, _ => new ConcurrentDictionary<string, ModSummary>(StringComparer.OrdinalIgnoreCase));
                    foreach (var mod in list)
                    {
                        dict[mod.Key.ExternalId] = mod;
                    }
                    _logger.LogInformation("Loaded {Count} packages for {ProviderId} from disk cache", list.Count, providerId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load catalog cache from {File}", file);
            }
        }
    }

    public async Task RefreshAllAsync(CancellationToken ct = default)
    {
        foreach (var provider in _providers)
        {
            if (provider.Capabilities.HasFlag(ProviderCapabilities.FullIndex))
            {
                try
                {
                    await provider.RefreshCatalogAsync(this, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to refresh provider {ProviderId}", provider.Id);
                }
            }
        }
    }

    public IReadOnlyList<ModSummary> Search(ModQuery query)
    {
        IEnumerable<ModSummary> allMods;

        if (!string.IsNullOrWhiteSpace(query.ProviderId) && _catalog.TryGetValue(query.ProviderId, out var dict))
        {
            allMods = dict.Values;
        }
        else
        {
            // If querying across providers, group by CanonicalId to avoid duplicating identical mods
            allMods = _catalog.Values.SelectMany(d => d.Values);
        }

        if (!query.IncludeDeprecated)
        {
            allMods = allMods.Where(m => !m.IsDeprecated);
        }

        if (!string.IsNullOrWhiteSpace(query.SearchText))
        {
            var search = query.SearchText.Trim();
            allMods = allMods.Where(m =>
                m.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                m.Owner.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                m.Description.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(query.Author))
        {
            allMods = allMods.Where(m => m.Owner.Equals(query.Author, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(query.Category))
        {
            allMods = allMods.Where(m => m.Categories.Any(c => c.Equals(query.Category, StringComparison.OrdinalIgnoreCase)));
        }

        // Distinct by CanonicalId to present a clean unified list
        var deduplicated = allMods
            .GroupBy(m => m.CanonicalId)
            .Select(g => g.OrderByDescending(m => m.Key.ProviderId == "thunderstore" ? 1 : 0).First())
            .OrderByDescending(m => m.IsPinned)
            .ThenByDescending(m => m.TotalDownloads)
            .ToList();

        if (query.PageSize > 0)
        {
            return deduplicated
                .Skip(query.PageIndex * query.PageSize)
                .Take(query.PageSize)
                .ToList();
        }

        return deduplicated;
    }

    public IReadOnlyList<GroupedModSummary> SearchGrouped(ModQuery query)
    {
        IEnumerable<ModSummary> allMods;

        if (!string.IsNullOrWhiteSpace(query.ProviderId) && _catalog.TryGetValue(query.ProviderId, out var dict))
        {
            allMods = dict.Values;
        }
        else
        {
            allMods = _catalog.Values.SelectMany(d => d.Values);
        }

        if (!query.IncludeDeprecated)
        {
            allMods = allMods.Where(m => !m.IsDeprecated);
        }

        if (!string.IsNullOrWhiteSpace(query.SearchText))
        {
            var search = query.SearchText.Trim();
            allMods = allMods.Where(m =>
                m.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                m.Owner.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                m.Description.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(query.Author))
        {
            allMods = allMods.Where(m => m.Owner.Equals(query.Author, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(query.Category))
        {
            allMods = allMods.Where(m => m.Categories.Any(c => c.Equals(query.Category, StringComparison.OrdinalIgnoreCase)));
        }

        var grouped = allMods
            .GroupBy(m => m.CanonicalId)
            .Select(g =>
            {
                var summaries = g.ToList();

                string highestVersion = "0.0.0";
                foreach (var s in summaries)
                {
                    if (ModVersionComparer.IsNewer(s.LatestVersionNumber, highestVersion))
                    {
                        highestVersion = s.LatestVersionNumber;
                    }
                }

                var sources = summaries.Select(s =>
                {
                    var displayName = s.Key.ProviderId.Equals("hexium", StringComparison.OrdinalIgnoreCase)
                        ? "Hexium"
                        : s.Key.ProviderId.Equals("thunderstore", StringComparison.OrdinalIgnoreCase)
                            ? "Thunderstore"
                            : s.Key.ProviderId;

                    var isNewest = s.LatestVersionNumber == highestVersion ||
                                   !ModVersionComparer.IsNewer(highestVersion, s.LatestVersionNumber);

                    return new ModSourceRelease(
                        ProviderId: s.Key.ProviderId,
                        DisplayName: displayName,
                        Summary: s,
                        LatestVersion: s.LatestVersionNumber,
                        IsNewestOverall: isNewest
                    );
                })
                .OrderByDescending(s => s.IsNewestOverall)
                .ThenByDescending(s => s.ProviderId == "thunderstore" ? 1 : 0)
                .ToList();

                var primary = sources[0].Summary;

                return new GroupedModSummary(
                    CanonicalId: g.Key,
                    Name: primary.Name,
                    Owner: primary.Owner,
                    Description: primary.Description,
                    IconUrl: primary.IconUrl,
                    HighestVersionOverall: highestVersion,
                    TotalDownloadsOverall: summaries.Sum(s => s.TotalDownloads),
                    HighestRatingOverall: summaries.Max(s => s.RatingScore),
                    IsPinned: summaries.Any(s => s.IsPinned),
                    IsDeprecated: summaries.All(s => s.IsDeprecated),
                    Categories: summaries.SelectMany(s => s.Categories).Distinct().ToList(),
                    AvailableSources: sources,
                    DateUpdatedOverall: summaries.Max(s => s.DateUpdated)
                );
            });

        var sorted = (query.SortBy, query.SortDescending) switch
        {
            (ModSortBy.Relevance, true) => grouped
                .OrderByDescending(m => GetRelevanceScore(m.Name, m.Owner, query.SearchText))
                .ThenByDescending(m => m.TotalDownloadsOverall),
            (ModSortBy.Relevance, false) => grouped
                .OrderBy(m => GetRelevanceScore(m.Name, m.Owner, query.SearchText))
                .ThenBy(m => m.TotalDownloadsOverall),
            (ModSortBy.Downloads, true) => string.IsNullOrWhiteSpace(query.SearchText)
                ? grouped.OrderByDescending(m => m.IsPinned).ThenByDescending(m => m.TotalDownloadsOverall)
                : grouped.OrderByDescending(m => m.TotalDownloadsOverall),
            (ModSortBy.Downloads, false) => grouped
                .OrderBy(m => m.TotalDownloadsOverall),
            (ModSortBy.LastUpdated, true) => grouped
                .OrderByDescending(m => m.DateUpdatedOverall)
                .ThenByDescending(m => m.TotalDownloadsOverall),
            (ModSortBy.LastUpdated, false) => grouped
                .OrderBy(m => m.DateUpdatedOverall)
                .ThenBy(m => m.TotalDownloadsOverall),
            (ModSortBy.Rating, true) => grouped
                .OrderByDescending(m => m.HighestRatingOverall)
                .ThenByDescending(m => m.TotalDownloadsOverall),
            (ModSortBy.Rating, false) => grouped
                .OrderBy(m => m.HighestRatingOverall)
                .ThenBy(m => m.TotalDownloadsOverall),
            (ModSortBy.Name, false) => grouped
                .OrderBy(m => m.Name, StringComparer.OrdinalIgnoreCase),
            (ModSortBy.Name, true) => grouped
                .OrderByDescending(m => m.Name, StringComparer.OrdinalIgnoreCase),
            (ModSortBy.Author, false) => grouped
                .OrderBy(m => m.Owner, StringComparer.OrdinalIgnoreCase)
                .ThenBy(m => m.Name, StringComparer.OrdinalIgnoreCase),
            (ModSortBy.Author, true) => grouped
                .OrderByDescending(m => m.Owner, StringComparer.OrdinalIgnoreCase)
                .ThenBy(m => m.Name, StringComparer.OrdinalIgnoreCase),
            _ => grouped
                .OrderByDescending(m => m.IsPinned)
                .ThenByDescending(m => m.TotalDownloadsOverall)
        };

        var resultList = sorted.ToList();

        if (query.PageSize > 0)
        {
            return resultList
                .Skip(query.PageIndex * query.PageSize)
                .Take(query.PageSize)
                .ToList();
        }

        return resultList;
    }

    private static int GetRelevanceScore(string name, string owner, string? searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText)) return 0;
        var s = searchText.Trim();

        if (name.Equals(s, StringComparison.OrdinalIgnoreCase)) return 4;
        if (name.StartsWith(s, StringComparison.OrdinalIgnoreCase)) return 3;
        if (name.Contains(s, StringComparison.OrdinalIgnoreCase)) return 2;
        if (owner.Contains(s, StringComparison.OrdinalIgnoreCase)) return 1;
        return 0;
    }

    public ModSummary? FindByCanonicalId(CanonicalModId canonicalId, string? preferredProvider = "thunderstore")
    {
        var matches = new List<ModSummary>();
        foreach (var dict in _catalog.Values)
        {
            var match = dict.Values.FirstOrDefault(m => m.CanonicalId == canonicalId);
            if (match != null)
            {
                matches.Add(match);
            }
        }

        if (matches.Count == 0) return null;
        if (matches.Count == 1) return matches[0];

        string highestVersion = "0.0.0";
        foreach (var m in matches)
        {
            if (ModVersionComparer.IsNewer(m.LatestVersionNumber, highestVersion))
            {
                highestVersion = m.LatestVersionNumber;
            }
        }

        return matches
            .Where(m => m.LatestVersionNumber == highestVersion || !ModVersionComparer.IsNewer(highestVersion, m.LatestVersionNumber))
            .OrderByDescending(m => preferredProvider != null && m.Key.ProviderId.Equals(preferredProvider, StringComparison.OrdinalIgnoreCase) ? 2 : 0)
            .ThenByDescending(m => m.Key.ProviderId.Equals("thunderstore", StringComparison.OrdinalIgnoreCase) ? 1 : 0)
            .First();
    }

    public ModSummary? FindByKey(ModKey key)
    {
        if (_catalog.TryGetValue(key.ProviderId, out var dict) &&
            dict.TryGetValue(key.ExternalId, out var mod))
        {
            return mod;
        }
        return null;
    }

    public int TotalPackageCount => _catalog.Values.Sum(v => v.Count);
}
