namespace ValheimModManager.Core.Providers.Thunderstore;

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ValheimModManager.Core.Abstractions;
using ValheimModManager.Core.Models;

public sealed class ThunderstoreCompatibleProvider : IModProvider
{
    private readonly ThunderstoreSourceOptions _options;
    private readonly HttpClient _httpClient;
    private readonly ILogger<ThunderstoreCompatibleProvider> _logger;

    public ThunderstoreCompatibleProvider(
        ThunderstoreSourceOptions options,
        HttpClient httpClient,
        ILogger<ThunderstoreCompatibleProvider>? logger = null)
    {
        _options = options;
        _httpClient = httpClient;
        _logger = logger ?? NullLogger<ThunderstoreCompatibleProvider>.Instance;
    }

    public string Id => _options.ProviderId;
    public string DisplayName => _options.DisplayName;
    public ProviderCapabilities Capabilities => ProviderCapabilities.FullIndex;

    public async Task RefreshCatalogAsync(ICatalogSink sink, CancellationToken ct = default)
    {
        _logger.LogInformation("Refreshing catalog from {Provider} ({BaseUri})", DisplayName, _options.BaseUri);
        var indexUri = new Uri(_options.BaseUri, "package-listing-index/");

        List<string> chunkUrls;
        using (var response = await _httpClient.GetAsync(indexUri, HttpCompletionOption.ResponseHeadersRead, ct))
        {
            response.EnsureSuccessStatusCode();
            await using var rawStream = await response.Content.ReadAsStreamAsync(ct);
            await using var decompressedStream = await DecompressIfNeededAsync(rawStream);
            chunkUrls = await JsonSerializer.DeserializeAsync<List<string>>(decompressedStream, cancellationToken: ct) ?? [];
        }

        _logger.LogInformation("Found {Count} chunks for {Provider}", chunkUrls.Count, DisplayName);
        var totalLoaded = 0;

        foreach (var chunkUrl in chunkUrls)
        {
            ct.ThrowIfCancellationRequested();
            var chunkUri = new Uri(chunkUrl);

            using var chunkResponse = await _httpClient.GetAsync(chunkUri, HttpCompletionOption.ResponseHeadersRead, ct);
            chunkResponse.EnsureSuccessStatusCode();

            await using var rawChunk = await chunkResponse.Content.ReadAsStreamAsync(ct);
            await using var decompressedChunk = await DecompressIfNeededAsync(rawChunk);

            var items = JsonSerializer.DeserializeAsyncEnumerable<ThunderstorePackageDto>(
                decompressedChunk,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
                cancellationToken: ct);

            await foreach (var item in items)
            {
                if (item == null || string.IsNullOrWhiteSpace(item.Name)) continue;

                var summary = MapToSummary(item);
                await sink.UpsertPackageAsync(Id, summary, ct);
                totalLoaded++;
            }
        }

        await sink.CompleteProviderRefreshAsync(Id, totalLoaded, ct);
        _logger.LogInformation("Completed {Provider} catalog refresh with {Count} packages", DisplayName, totalLoaded);
    }

    public IAsyncEnumerable<ModSummary> SearchAsync(ModQuery query, CancellationToken ct = default)
    {
        // FullIndex providers rely on CatalogService for local querying
        return AsyncEnumerable.Empty<ModSummary>();
    }

    public Task<ModDetails?> GetDetailsAsync(ModKey key, CancellationToken ct = default)
    {
        // For Thunderstore/Hexium, details are resolved from the catalog cache or experimental package API
        return Task.FromResult<ModDetails?>(null);
    }

    public Task<DownloadTicket> ResolveDownloadAsync(ModVersionRef versionRef, DownloadContext context, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(versionRef.DownloadUrl))
        {
            throw new InvalidOperationException($"No download URL provided for {versionRef.ModKey} v{versionRef.VersionNumber}");
        }

        var fileName = $"{versionRef.ModKey.ExternalId}-{versionRef.VersionNumber}.zip";
        return Task.FromResult(new DownloadTicket(new Uri(versionRef.DownloadUrl), fileName));
    }

    private ModSummary MapToSummary(ThunderstorePackageDto dto)
    {
        var versions = dto.Versions.Select(v => new ModVersion(
            v.VersionNumber,
            v.Description,
            v.Icon,
            v.DownloadUrl,
            v.FileSize,
            v.DateCreated,
            v.Dependencies.Select(Dependency.Parse).ToList()
        )).ToList();

        var latestVersion = versions.FirstOrDefault()?.VersionNumber ?? "1.0.0";
        var iconUrl = versions.FirstOrDefault()?.IconUrl ?? "";
        var description = versions.FirstOrDefault()?.Description ?? "";

        var canonical = new CanonicalModId(dto.Owner, dto.Name);
        var key = new ModKey(Id, dto.FullName);

        return new ModSummary(
            Key: key,
            CanonicalId: canonical,
            Name: dto.Name,
            Owner: dto.Owner,
            PackageUrl: dto.PackageUrl,
            Description: description,
            IconUrl: iconUrl,
            LatestVersionNumber: latestVersion,
            TotalDownloads: dto.Versions.Sum(v => v.Downloads),
            RatingScore: dto.RatingScore,
            IsPinned: dto.IsPinned,
            IsDeprecated: dto.IsDeprecated,
            DateUpdated: dto.DateUpdated,
            Categories: dto.Categories,
            Versions: versions
        );
    }

    private static async Task<Stream> DecompressIfNeededAsync(Stream stream)
    {
        var mem = new MemoryStream();
        await stream.CopyToAsync(mem);
        mem.Position = 0;

        // Check for GZip magic header 0x1F, 0x8B
        if (mem.Length >= 2)
        {
            var b1 = mem.ReadByte();
            var b2 = mem.ReadByte();
            mem.Position = 0;

            if (b1 == 0x1F && b2 == 0x8B)
            {
                var decompressed = new MemoryStream();
                await using var gzip = new GZipStream(mem, CompressionMode.Decompress, leaveOpen: true);
                await gzip.CopyToAsync(decompressed);
                decompressed.Position = 0;
                return decompressed;
            }
        }

        return mem;
    }
}
