namespace ValheimModManager.Core.Models;

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

[JsonConverter(typeof(DependencyJsonConverter))]
public readonly record struct Dependency(string RawIdentifier)
{
    public Dependency(string rawIdentifier, string? ns, string? name, string? versionRequirement)
        : this(rawIdentifier)
    {
    }

    public string? Namespace
    {
        get
        {
            var span = RawIdentifier.AsSpan();
            var idx = span.IndexOf('-');
            return idx > 0 ? span[..idx].ToString() : null;
        }
    }

    public string Name
    {
        get
        {
            var span = RawIdentifier.AsSpan();
            var first = span.IndexOf('-');
            var last = span.LastIndexOf('-');
            if (first > 0 && last > first)
            {
                return span.Slice(first + 1, last - first - 1).ToString();
            }
            return RawIdentifier;
        }
    }

    public string? VersionRequirement
    {
        get
        {
            var span = RawIdentifier.AsSpan();
            var last = span.LastIndexOf('-');
            return last > 0 && last < span.Length - 1 ? span[(last + 1)..].ToString() : null;
        }
    }

    public static Dependency Parse(string raw) => new(DependencyJsonConverter.Intern(raw));
}

public sealed class DependencyJsonConverter : JsonConverter<Dependency>
{
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, string> StringPool = new(StringComparer.Ordinal);

    public static string Intern(string? s)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;
        return StringPool.GetOrAdd(s, s);
    }

    public static void ClearPool() => StringPool.Clear();

    public override Dependency Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var s = reader.GetString();
            return new Dependency(Intern(s));
        }

        if (reader.TokenType == JsonTokenType.StartObject)
        {
            string? raw = null;
            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    var prop = reader.GetString();
                    reader.Read();
                    var val = reader.TokenType == JsonTokenType.String ? reader.GetString() : null;
                    if (string.Equals(prop, "RawIdentifier", StringComparison.OrdinalIgnoreCase)) raw = val;
                }
            }

            return new Dependency(Intern(raw));
        }

        throw new JsonException($"Unexpected token {reader.TokenType} when parsing Dependency.");
    }

    public override void Write(Utf8JsonWriter writer, Dependency value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.RawIdentifier);
    }
}

public sealed record ModVersion(
    string VersionNumber,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Description,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? IconUrl,
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
    IReadOnlyList<ModSourceRelease> AvailableSources,
    DateTime DateUpdatedOverall = default
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
