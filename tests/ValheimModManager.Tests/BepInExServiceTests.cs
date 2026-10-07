namespace ValheimModManager.Tests;

using System;
using System.IO;
using System.IO.Compression;
using ValheimModManager.Core.Models;
using ValheimModManager.Core.Services;
using Xunit;

public class BepInExServiceTests : IDisposable
{
    private readonly string _testDir;
    private readonly string _gameDir;
    private readonly string _profileDir;

    public BepInExServiceTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "VMM_Tests_Bep_" + Guid.NewGuid().ToString("N"));
        _gameDir = Path.Combine(_testDir, "ValheimGame");
        _profileDir = Path.Combine(_testDir, "Profile1");

        Directory.CreateDirectory(_gameDir);
        Directory.CreateDirectory(_profileDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
        {
            Directory.Delete(_testDir, recursive: true);
        }
    }

    [Fact]
    public void BepInExService_ConfiguresDoorstopAndRestoresVanilla()
    {
        var service = new BepInExService();
        var game = new GameInstall(GameTarget.Client, _gameDir, Path.Combine(_gameDir, "valheim.exe"));

        // Status before configuration
        var statusBefore = service.GetStatus(game, _profileDir);
        Assert.False(statusBefore.IsGameConfigured);
        Assert.False(statusBefore.IsInstalledInProfile);

        // Configure doorstop
        service.ConfigureDoorstopForProfile(game, _profileDir);

        var doorstopPath = Path.Combine(_gameDir, "doorstop_config.ini");
        Assert.True(File.Exists(doorstopPath));
        var content = File.ReadAllText(doorstopPath);
        Assert.Contains("target_assembly =", content);
        Assert.Contains(Path.Combine(_profileDir, "BepInEx", "core", "BepInEx.Preloader.dll"), content);

        // Mock winhttp.dll and core preloader
        File.WriteAllText(Path.Combine(_gameDir, "winhttp.dll"), "mock");
        var preloaderDir = Path.Combine(_profileDir, "BepInEx", "core");
        Directory.CreateDirectory(preloaderDir);
        File.WriteAllText(Path.Combine(preloaderDir, "BepInEx.Preloader.dll"), "mock");

        var statusAfter = service.GetStatus(game, _profileDir);
        Assert.True(statusAfter.IsGameConfigured);
        Assert.True(statusAfter.IsInstalledInProfile);

        // Restore vanilla
        service.RestoreVanilla(game);
        Assert.False(File.Exists(Path.Combine(_gameDir, "winhttp.dll")));
        Assert.False(File.Exists(Path.Combine(_gameDir, "doorstop_config.ini")));
    }

    [Theory]
    [InlineData("5.4.2202.0", "5.4.2202.0+13a7b9c", "5.4.2202.0")]
    [InlineData("5.4.22.0", null, "5.4.22.0")]
    [InlineData(null, "5.4.2202.0+abcdef123456789", "5.4.2202.0")]
    [InlineData("", "5.4.21.0", "5.4.21.0")]
    [InlineData("   ", "5.4.20.0+meta+data", "5.4.20.0")]
    [InlineData(null, null, null)]
    public void BepInExService_CleanVersionInfo_PrioritizesFileVersionAndStripsMetadata(
        string? fileVersion,
        string? productVersion,
        string? expected)
    {
        var result = BepInExService.CleanVersionInfo(fileVersion, productVersion);
        Assert.Equal(expected, result);
    }
}
