namespace ValheimModManager.Core.Import;

using System;
using System.Collections.Generic;

public sealed class R2ModVersion
{
    public int Major { get; set; }
    public int Minor { get; set; }
    public int Patch { get; set; }

    public override string ToString() => $"{Major}.{Minor}.{Patch}";
}

public sealed class R2ModEntry
{
    public string Name { get; set; } = string.Empty;
    public R2ModVersion? Version { get; set; }
    public bool Enabled { get; set; } = true;

    public string VersionString => Version?.ToString() ?? "1.0.0";
}

public sealed class R2ProfileExport
{
    public string ProfileName { get; set; } = "Imported";
    public List<R2ModEntry> Mods { get; set; } = [];
}
