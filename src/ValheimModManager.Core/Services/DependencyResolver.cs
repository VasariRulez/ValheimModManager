namespace ValheimModManager.Core.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using ValheimModManager.Core.Models;

public sealed record ResolvedDependency(
    CanonicalModId CanonicalId,
    ModSummary Summary,
    string RequiredVersion,
    bool AlreadyInstalled
);

public sealed class DependencyResolver
{
    private readonly CatalogService _catalogService;

    public DependencyResolver(CatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    public List<ResolvedDependency> ResolveDependencies(
        ModSummary rootPackage,
        string rootVersionNumber,
        Profile profile)
    {
        var result = new List<ResolvedDependency>();
        var visited = new HashSet<CanonicalModId>();

        var installedMap = profile.Mods.ToDictionary(m => m.CanonicalId, m => m);

        void Traverse(ModSummary currentMod, string versionNumber)
        {
            var versionObj = currentMod.Versions.FirstOrDefault(v => v.VersionNumber == versionNumber)
                             ?? currentMod.Versions.FirstOrDefault();

            if (versionObj == null) return;

            foreach (var dep in versionObj.Dependencies)
            {
                if (string.IsNullOrWhiteSpace(dep.Name)) continue;

                var depCanonical = new CanonicalModId(dep.Namespace ?? "Default", dep.Name);

                // Ignore BepInExPack as it is managed by BepInExService as foundation
                if (depCanonical.Name.Contains("BepInExPack", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!visited.Add(depCanonical)) continue;

                var isInstalled = installedMap.ContainsKey(depCanonical);

                // Find package in catalog
                var depSummary = _catalogService.FindByCanonicalId(depCanonical);
                if (depSummary != null)
                {
                    // Recurse into dependencies of this dependency
                    Traverse(depSummary, dep.VersionRequirement ?? depSummary.LatestVersionNumber);

                    result.Add(new ResolvedDependency(
                        depCanonical,
                        depSummary,
                        dep.VersionRequirement ?? depSummary.LatestVersionNumber,
                        isInstalled
                    ));
                }
            }
        }

        Traverse(rootPackage, rootVersionNumber);
        return result;
    }
}
