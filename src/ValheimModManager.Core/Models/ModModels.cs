namespace ValheimModManager.Core.Models;

using System;
using System.Collections.Generic;

public sealed record Dependency(
    string RawIdentifier,
    string? Namespace,
    string? Name,
    string? VersionRequirement
)
{
    public static Dependency Parse(string raw)
    {
        // Thunderstore format: Author-ModName-1.2.3
        var parts = raw.Split('-');
        if (parts.Length >= 3)
        {
            var ns = parts[0];
            var ver = parts[^1];
            var name = string.Join('-', parts[1..^1]);
            return new Dependency(raw, ns, name, ver);
        }
        return new Dependency(raw, null, raw, null);
    }
}

public sealed record ModVersion(
    string VersionNumber,
    string Description,
    string IconUrl,
    string DownloadUrl,
    long FileSize,
    DateTime DateCreated,
    IReadOnlyList<Dependency> Dependencies
);

public sealed record ModVersionRef(
    ModKey ModKey,
    string VersionNumber,
    string? DownloadUrl = null
);

public sealed record ModSummary(
    ModKey Key,
    CanonicalModId CanonicalId,
    string Name,
    string Owner,
    string PackageUrl,
    string Description,
    string IconUrl,
    string LatestVersionNumber,
    long TotalDownloads,
    int RatingScore,
    bool IsPinned,
    bool IsDeprecated,
    DateTime DateUpdated,
    IReadOnlyList<string> Categories,
    IReadOnlyList<ModVersion> Versions
);

public sealed record ModSourceRelease(
    string ProviderId,
    string DisplayName,
    ModSummary Summary,
    string LatestVersion,
    bool IsNewestOverall
)
{
    public string BadgeText =>
        $"{(ProviderId.Equals("thunderstore", StringComparison.OrdinalIgnoreCase) ? "⚡" : "🔷")} {DisplayName} v{LatestVersion}";

    public override string ToString() =>
        IsNewestOverall
            ? $"{DisplayName} (v{LatestVersion} - Più recente ⭐)"
            : $"{DisplayName} (v{LatestVersion})";
}

public sealed record GroupedModSummary(
    CanonicalModId CanonicalId,
    string Name,
    string Owner,
    string Description,
    string IconUrl,
    string HighestVersionOverall,
    long TotalDownloadsOverall,
    int HighestRatingOverall,
    bool IsPinned,
    bool IsDeprecated,
    IReadOnlyList<string> Categories,
    IReadOnlyList<ModSourceRelease> AvailableSources
);

public sealed record ModDetails(
    ModKey Key,
    CanonicalModId CanonicalId,
    string Name,
    string Owner,
    string PackageUrl,
    string Description,
    string IconUrl,
    string? WebsiteUrl,
    string? ReadmeMarkdown,
    IReadOnlyList<string> Categories,
    IReadOnlyList<ModVersion> Versions
);
