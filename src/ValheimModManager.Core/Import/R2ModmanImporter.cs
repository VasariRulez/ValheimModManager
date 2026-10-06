namespace ValheimModManager.Core.Import;

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using ValheimModManager.Core.Install;
using ValheimModManager.Core.Models;
using ValheimModManager.Core.Services;

public sealed record R2ImportResult(
    Profile CreatedProfile,
    IReadOnlyList<R2ModEntry> DeclaredMods,
    IReadOnlyList<ModSummary> ResolvedCatalogMods,
    IReadOnlyList<string> UnresolvedModNames
);

public sealed class R2ModmanImporter
{
    private readonly HttpClient _httpClient;
    private readonly ProfileService _profileService;
    private readonly CatalogService _catalogService;
    private readonly ILogger<R2ModmanImporter> _logger;
    private readonly IDeserializer _yamlDeserializer;

    public R2ModmanImporter(
        HttpClient httpClient,
        ProfileService profileService,
        CatalogService catalogService,
        ILogger<R2ModmanImporter>? logger = null)
    {
        _httpClient = httpClient;
        _profileService = profileService;
        _catalogService = catalogService;
        _logger = logger ?? NullLogger<R2ModmanImporter>.Instance;

        _yamlDeserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
    }

    public async Task<R2ImportResult> ImportFromCodeAsync(
        string profileCode,
        string? targetProfileName = null,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Importing r2modman profile from code: {Code}", profileCode);
        var url = $"https://thunderstore.io/api/experimental/legacyprofile/get/{profileCode.Trim()}/";

        using var response = await _httpClient.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();

        var rawPayload = await response.Content.ReadAsStringAsync(ct);
        var base64 = rawPayload.Trim();
        if (base64.StartsWith("#r2modman", StringComparison.OrdinalIgnoreCase))
        {
            base64 = base64["#r2modman".Length..].Trim();
        }

        var zipBytes = Convert.FromBase64String(base64);
        var tempZip = Path.Combine(Path.GetTempPath(), $"r2_import_{Guid.NewGuid():N}.r2z");
        try
        {
            await File.WriteAllBytesAsync(tempZip, zipBytes, ct);
            return await ImportFromR2zAsync(tempZip, targetProfileName, ct);
        }
        finally
        {
            if (File.Exists(tempZip)) File.Delete(tempZip);
        }
    }

    public Task<R2ImportResult> ImportFromR2zAsync(
        string r2zFilePath,
        string? targetProfileName = null,
        CancellationToken ct = default)
    {
        using var archive = ZipFile.OpenRead(r2zFilePath);
        var exportEntry = archive.GetEntry("export.r2x");
        if (exportEntry == null)
        {
            throw new InvalidOperationException("Archive does not contain an 'export.r2x' file.");
        }

        string yamlContent;
        using (var reader = new StreamReader(exportEntry.Open()))
        {
            yamlContent = reader.ReadToEnd();
        }

        var exportData = _yamlDeserializer.Deserialize<R2ProfileExport>(yamlContent);
        var profileName = string.IsNullOrWhiteSpace(targetProfileName) ? exportData.ProfileName : targetProfileName;

        var profile = _profileService.CreateProfile(profileName);
        var profileDir = _profileService.GetProfileDirectory(profile.Name);

        // Extract configs from archive
        foreach (var entry in archive.Entries)
        {
            if (string.IsNullOrEmpty(entry.Name)) continue;

            if (entry.FullName.StartsWith("BepInEx/config/", StringComparison.OrdinalIgnoreCase) ||
                entry.FullName.StartsWith("config/", StringComparison.OrdinalIgnoreCase))
            {
                var rel = entry.FullName.StartsWith("BepInEx/", StringComparison.OrdinalIgnoreCase)
                    ? entry.FullName["BepInEx/".Length..]
                    : Path.Combine("config", entry.FullName["config/".Length..]);

                var dest = Path.Combine(profileDir, "BepInEx", rel.Replace('/', Path.DirectorySeparatorChar));
                var dir = Path.GetDirectoryName(dest);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                entry.ExtractToFile(dest, overwrite: true);
            }
        }

        // Match mods with catalog
        var resolvedMods = new List<ModSummary>();
        var unresolved = new List<string>();

        foreach (var entry in exportData.Mods)
        {
            var canonical = CanonicalModId.Parse(entry.Name);
            var catalogMatch = _catalogService.FindByCanonicalId(canonical);

            if (catalogMatch != null)
            {
                resolvedMods.Add(catalogMatch);
            }
            else
            {
                unresolved.Add(entry.Name);
            }
        }

        return Task.FromResult(new R2ImportResult(profile, exportData.Mods, resolvedMods, unresolved));
    }
}
