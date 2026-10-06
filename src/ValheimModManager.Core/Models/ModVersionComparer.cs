namespace ValheimModManager.Core.Models;

using System;
using System.Collections.Generic;
using Semver;

public sealed class ModVersionComparer : IComparer<string>
{
    public static readonly ModVersionComparer Instance = new();

    public int Compare(string? x, string? y)
    {
        if (x == null && y == null) return 0;
        if (x == null) return -1;
        if (y == null) return 1;

        if (SemVersion.TryParse(Clean(x), SemVersionStyles.Any, out var verX) &&
            SemVersion.TryParse(Clean(y), SemVersionStyles.Any, out var verY))
        {
            return verX.CompareSortOrderTo(verY);
        }

        // Fallback to Version if standard semver parse fails
        if (Version.TryParse(Clean(x), out var sysVerX) &&
            Version.TryParse(Clean(y), out var sysVerY))
        {
            return sysVerX.CompareTo(sysVerY);
        }

        return string.Compare(x, y, StringComparison.OrdinalIgnoreCase);
    }

    private static string Clean(string versionStr)
    {
        var trimmed = versionStr.Trim();
        if (trimmed.StartsWith('v') || trimmed.StartsWith('V'))
        {
            trimmed = trimmed[1..];
        }
        return trimmed;
    }

    public static bool IsNewer(string remoteVersion, string installedVersion)
    {
        return Instance.Compare(remoteVersion, installedVersion) > 0;
    }
}
