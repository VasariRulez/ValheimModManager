namespace ValheimModManager.Tests;

using System;
using System.IO;
using ValheimModManager.Core.Common;
using Xunit;

public class VdfParserTests : IDisposable
{
    private readonly string _testDir;

    public VdfParserTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "VMM_Tests_Vdf_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
        {
            Directory.Delete(_testDir, recursive: true);
        }
    }

    [Fact]
    public void VdfParser_ParsesValidLibraryFolders()
    {
        var dummyLib1 = Path.Combine(_testDir, "SteamLib1");
        var dummyLib2 = Path.Combine(_testDir, "SteamLib2");
        Directory.CreateDirectory(dummyLib1);
        Directory.CreateDirectory(dummyLib2);

        var vdf = $@"
""libraryfolders""
{{
    ""0""
    {{
        ""path""    ""{dummyLib1.Replace("\\", "\\\\")}""
        ""label""   """"
        ""contentid""   ""12345""
    }}
    ""1""
    {{
        ""path""    ""{dummyLib2.Replace("\\", "\\\\")}""
        ""label""   """"
        ""contentid""   ""67890""
    }}
}}
";

        var folders = VdfParser.ParseLibraryFolders(vdf);
        Assert.Equal(2, folders.Count);
        Assert.Contains(dummyLib1, folders);
        Assert.Contains(dummyLib2, folders);
    }
}
