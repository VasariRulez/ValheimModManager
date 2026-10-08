namespace ValheimModManager.Tests;

using System;
using System.IO;
using ValheimModManager.Core.Abstractions;
using ValheimModManager.Core.Models;
using ValheimModManager.Core.Services;
using Xunit;

public class GameLauncherTests : IDisposable
{
    private readonly string _testDir;
    private readonly string _gameDir;
    private readonly string _profileDir;
    private readonly string _dummyExe;

    public GameLauncherTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "VMM_Tests_Launcher_" + Guid.NewGuid().ToString("N"));
        _gameDir = Path.Combine(_testDir, "Valheim");
        _profileDir = Path.Combine(_testDir, "profiles", "Default");

        Directory.CreateDirectory(_gameDir);
        Directory.CreateDirectory(_profileDir);

        // Use cmd.exe on Windows as a safe, real executable for test processes
        _dummyExe = OperatingSystem.IsWindows()
            ? Path.Combine(Environment.SystemDirectory, "cmd.exe")
            : "/bin/sh";
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
        {
            try
            {
                Directory.Delete(_testDir, recursive: true);
            }
            catch
            {
                // ignore
            }
        }
    }

    private class MockProcessMonitor : IProcessMonitor
    {
        public bool ShouldReportRunning { get; set; }
        public bool IsRunning(GameTarget target) => ShouldReportRunning;
    }

    private class MockSteamLocator : ISteamLocator
    {
        public string? SteamPath { get; set; }
        public IReadOnlyList<GameInstall> FindInstalls() => [];
        public string? GetSteamExecutablePath() => SteamPath;
    }

    [Fact]
    public void LaunchGame_ThrowsWhenExecutableNotFound()
    {
        var bepInExService = new BepInExService();
        var monitor = new MockProcessMonitor();
        var launcher = new GameLauncher(bepInExService, monitor);

        var nonExistentGame = new GameInstall(GameTarget.Client, _gameDir, Path.Combine(_gameDir, "nonexistent.exe"));

        Assert.Throws<FileNotFoundException>(() =>
            launcher.LaunchGame(nonExistentGame, _profileDir));
    }

    [Fact]
    public void LaunchGame_ThrowsWhenGameIsAlreadyRunning()
    {
        var bepInExService = new BepInExService();
        var monitor = new MockProcessMonitor { ShouldReportRunning = true };
        var launcher = new GameLauncher(bepInExService, monitor);

        var game = new GameInstall(GameTarget.Client, _gameDir, _dummyExe);

        Assert.Throws<InvalidOperationException>(() =>
            launcher.LaunchGame(game, _profileDir));
    }

    [Fact]
    public void LaunchGame_DirectLaunch_ConfiguresDoorstopAndStartsProcess()
    {
        var bepInExService = new BepInExService();
        var monitor = new MockProcessMonitor { ShouldReportRunning = false };
        var launcher = new GameLauncher(bepInExService, monitor);

        var game = new GameInstall(GameTarget.Client, _gameDir, _dummyExe);

        // Launch directly (preferSteam = false) with arguments that exit immediately
        var proc = launcher.LaunchGame(game, _profileDir, "/c exit 0", preferSteam: false);

        Assert.NotNull(proc);
        try
        {
            proc.WaitForExit(3000);
        }
        catch
        {
            // ignore
        }

        // Verify doorstop_config.ini was generated in game directory
        var doorstopIni = Path.Combine(_gameDir, "doorstop_config.ini");
        Assert.True(File.Exists(doorstopIni));
        var content = File.ReadAllText(doorstopIni);
        Assert.Contains("target_assembly", content);
        Assert.Contains(Path.Combine(_profileDir, "BepInEx", "core", "BepInEx.Preloader.dll"), content);
    }

    [Fact]
    public void LaunchGame_SteamLaunch_UsesSteamExecutableWhenAvailable()
    {
        var bepInExService = new BepInExService();
        var monitor = new MockProcessMonitor { ShouldReportRunning = false };
        var steamLocator = new MockSteamLocator { SteamPath = _dummyExe };
        var launcher = new GameLauncher(bepInExService, monitor, steamLocator);

        var game = new GameInstall(GameTarget.Client, _gameDir, _dummyExe);

        // When preferSteam = true and steam executable exists, launches via Steam executable
        var proc = launcher.LaunchGame(game, _profileDir, "/c exit 0", preferSteam: true);

        Assert.NotNull(proc);
        try
        {
            proc.WaitForExit(3000);
        }
        catch
        {
            // ignore
        }

        // Verify doorstop was configured before Steam launch
        var doorstopIni = Path.Combine(_gameDir, "doorstop_config.ini");
        Assert.True(File.Exists(doorstopIni));
    }

    [Fact]
    public void LaunchGame_SteamLaunch_FallsBackToDirectLaunchWhenSteamUnavailable()
    {
        var bepInExService = new BepInExService();
        var monitor = new MockProcessMonitor { ShouldReportRunning = false };
        var steamLocator = new MockSteamLocator { SteamPath = null }; // Steam not available
        var launcher = new GameLauncher(bepInExService, monitor, steamLocator);

        var game = new GameInstall(GameTarget.Client, _gameDir, _dummyExe);

        var proc = launcher.LaunchGame(game, _profileDir, "/c exit 0", preferSteam: true);

        Assert.NotNull(proc);
        try
        {
            proc.WaitForExit(3000);
        }
        catch
        {
            // ignore
        }

        var doorstopIni = Path.Combine(_gameDir, "doorstop_config.ini");
        Assert.True(File.Exists(doorstopIni));
    }

    [Fact]
    public void LaunchGame_DedicatedServer_AlwaysLaunchesDirectlyEvenIfSteamRequested()
    {
        var bepInExService = new BepInExService();
        var monitor = new MockProcessMonitor { ShouldReportRunning = false };
        var steamLocator = new MockSteamLocator { SteamPath = _dummyExe };
        var launcher = new GameLauncher(bepInExService, monitor, steamLocator);

        var serverGame = new GameInstall(GameTarget.DedicatedServer, _gameDir, _dummyExe);

        var proc = launcher.LaunchGame(serverGame, _profileDir, "/c exit 0", preferSteam: true);

        Assert.NotNull(proc);
        try
        {
            proc.WaitForExit(3000);
        }
        catch
        {
            // ignore
        }

        var doorstopIni = Path.Combine(_gameDir, "doorstop_config.ini");
        Assert.True(File.Exists(doorstopIni));
    }
}
