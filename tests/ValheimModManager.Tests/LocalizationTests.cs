namespace ValheimModManager.Tests;

using System;
using System.IO;
using System.Reflection;
using ValheimModManager.Core.Localization;
using ValheimModManager.Core.Models;
using ValheimModManager.Core.Services;
using Xunit;

public class LocalizationTests : IDisposable
{
    private readonly string _testDir;

    public LocalizationTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "VMM_Tests_Localization_" + Guid.NewGuid().ToString("N"));
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
    public void ItalianStrings_AllPropertiesAreNonEmpty()
    {
        var it = ItalianStrings.Instance;
        var properties = typeof(AppStrings).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        Assert.True(properties.Length > 50, "AppStrings should have a comprehensive list of properties");

        foreach (var prop in properties)
        {
            if (prop.PropertyType == typeof(string))
            {
                var val = (string?)prop.GetValue(it);
                Assert.False(string.IsNullOrWhiteSpace(val), $"Italian property '{prop.Name}' is empty or whitespace");
            }
        }
    }

    [Fact]
    public void EnglishStrings_AllPropertiesAreNonEmpty()
    {
        var en = EnglishStrings.Instance;
        var properties = typeof(AppStrings).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        Assert.True(properties.Length > 50, "AppStrings should have a comprehensive list of properties");

        foreach (var prop in properties)
        {
            if (prop.PropertyType == typeof(string))
            {
                var val = (string?)prop.GetValue(en);
                Assert.False(string.IsNullOrWhiteSpace(val), $"English property '{prop.Name}' is empty or whitespace");
            }
        }
    }

    [Fact]
    public void ItalianAndEnglish_KeyStringsAreProperlyDistinct()
    {
        var it = ItalianStrings.Instance;
        var en = EnglishStrings.Instance;

        Assert.NotEqual(it.TabInstalledMods, en.TabInstalledMods);
        Assert.NotEqual(it.TabOnlineCatalog, en.TabOnlineCatalog);
        Assert.NotEqual(it.TabSettings, en.TabSettings);
        Assert.NotEqual(it.CommonSave, en.CommonSave);
        Assert.NotEqual(it.CommonCancel, en.CommonCancel);
        Assert.NotEqual(it.CommonClose, en.CommonClose);
        Assert.NotEqual(it.HeaderLaunchGameButton, en.HeaderLaunchGameButton);
        Assert.NotEqual(it.StatusGamePathConfigured, en.StatusGamePathConfigured);
        Assert.NotEqual(it.HeaderInstallBepInExShort, en.HeaderInstallBepInExShort);
        Assert.NotEqual(it.BepInExConfiguredShort, en.BepInExConfiguredShort);
        Assert.NotEqual(it.BepInExHooksPendingShort, en.BepInExHooksPendingShort);
        Assert.NotEqual(it.HeaderTooltipProfileOptions, en.HeaderTooltipProfileOptions);
    }

    [Theory]
    [InlineData("it", "it")]
    [InlineData("IT", "it")]
    [InlineData(" it ", "it")]
    [InlineData("en", "en")]
    [InlineData("EN", "en")]
    [InlineData(" en ", "en")]
    public void LocalizationService_NormalizeLanguageCode_Works(string input, string expected)
    {
        var service = LocalizationService.Instance;
        Assert.Equal(expected, service.NormalizeLanguageCode(input));
    }

    [Fact]
    public void LocalizationService_ResolvesCorrectStrings()
    {
        var service = LocalizationService.Instance;

        var itStrings = service.GetStrings("it");
        var enStrings = service.GetStrings("en");

        Assert.IsType<ItalianStrings>(itStrings);
        Assert.IsType<EnglishStrings>(enStrings);
    }

    [Fact]
    public void LocalizationService_AvailableLanguagesContainsItAndEn()
    {
        var langs = LocalizationService.Instance.AvailableLanguages;

        Assert.Contains(langs, l => l.Code == "it");
        Assert.Contains(langs, l => l.Code == "en");
    }

    [Fact]
    public void ProfileService_SavesAndRetrievesLanguagePreference()
    {
        var profilesDir = Path.Combine(_testDir, "profiles");
        var profileService = new ProfileService(profilesDir);

        var initialState = profileService.LoadState();
        Assert.Null(initialState.Language);

        profileService.SaveState(initialState with { Language = "en" });

        var reloaded = profileService.LoadState();
        Assert.Equal("en", reloaded.Language);

        profileService.SaveState(reloaded with { Language = "it" });
        var reloadedIt = profileService.LoadState();
        Assert.Equal("it", reloadedIt.Language);
    }
}
