namespace ValheimModManager.Tests;

using System;
using System.IO;
using ValheimModManager.Core.Models;
using ValheimModManager.Core.Services;
using Xunit;

public class ProfileServiceTests : IDisposable
{
    private readonly string _testDir;

    public ProfileServiceTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "VMM_Tests_Profiles_" + Guid.NewGuid().ToString("N"));
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
    public void ProfileService_CreatesClonesAndDeletesProfiles()
    {
        var service = new ProfileService(_testDir);

        // Default profile should exist
        var names = service.ListProfileNames();
        Assert.Contains("Default", names);

        // Create new profile
        var p2 = service.CreateProfile("Hardcore");
        Assert.Equal("Hardcore", p2.Name);
        Assert.True(Directory.Exists(service.GetProfileDirectory("Hardcore")));

        // Clone profile
        var p3 = service.CloneProfile("Hardcore", "Hardcore-Backup");
        Assert.Equal("Hardcore-Backup", p3.Name);
        Assert.True(Directory.Exists(service.GetProfileDirectory("Hardcore-Backup")));

        // Switch active profile
        service.SetActiveProfile("Hardcore");
        Assert.Equal("Hardcore", service.GetActiveProfileName());

        // Delete profile
        service.DeleteProfile("Hardcore-Backup");
        Assert.DoesNotContain("Hardcore-Backup", service.ListProfileNames());
    }

    [Fact]
    public void ProfileService_SavesAndRetrievesCustomLaunchArgs()
    {
        var service = new ProfileService(_testDir);

        // Initially null
        var state = service.LoadState();
        Assert.Null(state.CustomLaunchArgs);

        // Save custom launch args
        service.SaveState(state with { CustomLaunchArgs = "-console -window-mode exclusive" });

        // Reload and verify
        var reloaded = service.LoadState();
        Assert.Equal("-console -window-mode exclusive", reloaded.CustomLaunchArgs);

        // Set to null
        service.SaveState(reloaded with { CustomLaunchArgs = null });
        var cleared = service.LoadState();
        Assert.Null(cleared.CustomLaunchArgs);
    }
}
