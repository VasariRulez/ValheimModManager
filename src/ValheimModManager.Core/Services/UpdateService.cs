namespace ValheimModManager.Core.Services;

using System.Collections.Generic;
using System.Linq;
using ValheimModManager.Core.Models;

public sealed record ModUpdateCheckResult(
    InstalledMod InstalledMod,
    ModSummary? CatalogSummary,
    string LatestVersion,
    bool HasUpdate
);

public sealed class UpdateService
{
    private readonly CatalogService _catalogService;

    public UpdateService(CatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    public IReadOnlyList<ModUpdateCheckResult> CheckUpdates(Profile profile)
    {
        var results = new List<ModUpdateCheckResult>();

        foreach (var installed in profile.Mods)
        {
            var summary = _catalogService.FindByCanonicalId(installed.CanonicalId, installed.Key.ProviderId);
            var latestVer = summary?.LatestVersionNumber ?? installed.InstalledVersion;
            var hasUpdate = summary != null && ModVersionComparer.IsNewer(latestVer, installed.InstalledVersion);

            results.Add(new ModUpdateCheckResult(
                InstalledMod: installed,
                CatalogSummary: summary,
                LatestVersion: latestVer,
                HasUpdate: hasUpdate
            ));
        }

        return results;
    }
}
