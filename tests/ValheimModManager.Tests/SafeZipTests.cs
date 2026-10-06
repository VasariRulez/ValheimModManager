namespace ValheimModManager.Tests;

using System;
using System.IO;
using System.IO.Compression;
using ValheimModManager.Core.Install;
using Xunit;

public class SafeZipTests : IDisposable
{
    private readonly string _testDir;

    public SafeZipTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "VMM_Tests_SafeZip_" + Guid.NewGuid().ToString("N"));
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
    public void SafeZip_ExtractsNormalArchiveCorrectly()
    {
        var zipPath = Path.Combine(_testDir, "normal.zip");
        using (var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
        {
            var entry = archive.CreateEntry("subfolder/file.txt");
            using var writer = new StreamWriter(entry.Open());
            writer.Write("hello world");
        }

        var extractDir = Path.Combine(_testDir, "extracted");
        using (var archive = ZipFile.OpenRead(zipPath))
        {
            var files = SafeZip.ExtractArchiveSafe(archive, extractDir);
            Assert.Single(files);
            Assert.True(File.Exists(files[0]));
            Assert.Equal("hello world", File.ReadAllText(files[0]));
        }
    }

    [Fact]
    public void SafeZip_ThrowsOnZipSlipAttempt()
    {
        var zipPath = Path.Combine(_testDir, "malicious.zip");
        using (var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
        {
            var entry = archive.CreateEntry("../../../evil.txt");
            using var writer = new StreamWriter(entry.Open());
            writer.Write("evil payload");
        }

        var extractDir = Path.Combine(_testDir, "target");
        using var archiveRead = ZipFile.OpenRead(zipPath);

        Assert.Throws<InvalidOperationException>(() =>
        {
            SafeZip.ExtractArchiveSafe(archiveRead, extractDir);
        });
    }
}
