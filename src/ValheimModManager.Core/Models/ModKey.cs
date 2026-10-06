namespace ValheimModManager.Core.Models;

public readonly record struct ModKey(string ProviderId, string ExternalId)
{
    public override string ToString() => $"{ProviderId}:{ExternalId}";

    public static ModKey Parse(string raw)
    {
        var idx = raw.IndexOf(':');
        if (idx < 0) return new ModKey("unknown", raw);
        return new ModKey(raw[..idx], raw[(idx + 1)..]);
    }
}

public sealed record CanonicalModId(string Namespace, string Name)
{
    public override string ToString() => $"{Namespace}-{Name}";

    public static CanonicalModId Parse(string raw)
    {
        var idx = raw.IndexOf('-');
        if (idx < 0) return new CanonicalModId("Default", raw);
        return new CanonicalModId(raw[..idx], raw[(idx + 1)..]);
    }
}
