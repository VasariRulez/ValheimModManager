namespace ValheimModManager.Core.Common;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

public static partial class VdfParser
{
    private static readonly Regex PathRegex = new(
        @"\""path\""\s+\""([^\""]+)\""",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static List<string> ParseLibraryFolders(string vdfContent)
    {
        var folders = new List<string>();
        using var reader = new StringReader(vdfContent);
        string? line;

        while ((line = reader.ReadLine()) != null)
        {
            var match = PathRegex.Match(line);
            if (match.Success)
            {
                var pathValue = match.Groups[1].Value.Replace("\\\\", "\\");
                if (Directory.Exists(pathValue))
                {
                    folders.Add(pathValue);
                }
            }
        }

        return folders;
    }
}
