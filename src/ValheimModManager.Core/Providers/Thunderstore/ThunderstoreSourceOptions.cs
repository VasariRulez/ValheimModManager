namespace ValheimModManager.Core.Providers.Thunderstore;

using System;

public sealed record ThunderstoreSourceOptions(
    string ProviderId,
    string DisplayName,
    Uri BaseUri
)
{
    public static ThunderstoreSourceOptions Thunderstore =>
        new("thunderstore", "Thunderstore", new("https://thunderstore.io/c/valheim/api/v1/"));

    public static ThunderstoreSourceOptions Hexium =>
        new("hexium", "Hexium", new("https://valheim.hexium.gg/api/v1/"));
}
