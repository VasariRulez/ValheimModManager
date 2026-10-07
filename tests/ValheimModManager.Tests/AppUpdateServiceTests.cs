namespace ValheimModManager.Tests;

using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using ValheimModManager.Core.Services;
using Xunit;

public class AppUpdateServiceTests
{
    private const string SampleGitHubReleaseJson = """
    {
      "tag_name": "v1.0.4",
      "name": "Release v1.0.4",
      "body": "Bug fixes and launch argument improvements.",
      "html_url": "https://github.com/VasariRulez/ValheimModManager/releases/tag/v1.0.4",
      "published_at": "2026-10-06T20:37:47Z",
      "assets": [
        {
          "name": "SHA256SUMS.txt",
          "browser_download_url": "https://github.com/VasariRulez/ValheimModManager/releases/download/v1.0.4/SHA256SUMS.txt",
          "size": 107,
          "content_type": "text/plain"
        },
        {
          "name": "ValheimModManager-1.0.4-linux-x64.tar.gz",
          "browser_download_url": "https://github.com/VasariRulez/ValheimModManager/releases/download/v1.0.4/ValheimModManager-1.0.4-linux-x64.tar.gz",
          "size": 59000000,
          "content_type": "application/gzip"
        },
        {
          "name": "ValheimModManager-1.0.4-win-x64.zip",
          "browser_download_url": "https://github.com/VasariRulez/ValheimModManager/releases/download/v1.0.4/ValheimModManager-1.0.4-win-x64.zip",
          "size": 61000000,
          "content_type": "application/zip"
        }
      ]
    }
    """;

    [Fact]
    public void ParseReleaseJson_ParsesCorrectly()
    {
        var release = AppUpdateService.ParseReleaseJson(SampleGitHubReleaseJson);

        Assert.NotNull(release);
        Assert.Equal("v1.0.4", release.TagName);
        Assert.Equal("1.0.4", release.Version);
        Assert.Equal("Release v1.0.4", release.Title);
        Assert.Equal("Bug fixes and launch argument improvements.", release.ReleaseNotes);
        Assert.Equal(3, release.Assets.Count);
    }

    [Theory]
    [InlineData("1.0.2", true)]
    [InlineData("1.0.3", true)]
    [InlineData("1.0.4", false)]
    [InlineData("1.0.5", false)]
    public void CheckVersionComparison_DetectsNewer(string currentVersion, bool shouldHaveUpdate)
    {
        var release = AppUpdateService.ParseReleaseJson(SampleGitHubReleaseJson);
        Assert.NotNull(release);

        var isNewer = ValheimModManager.Core.Models.ModVersionComparer.IsNewer(release.Version, currentVersion);
        Assert.Equal(shouldHaveUpdate, isNewer);
    }

    [Fact]
    public void SelectAssetForCurrentPlatform_SelectsAppropriatePackage()
    {
        var service = new AppUpdateService();
        var release = AppUpdateService.ParseReleaseJson(SampleGitHubReleaseJson);
        Assert.NotNull(release);

        var asset = service.SelectAssetForCurrentPlatform(release);
        Assert.NotNull(asset);

        if (OperatingSystem.IsWindows())
        {
            Assert.Contains("win-x64", asset.Name);
            Assert.EndsWith(".zip", asset.Name);
        }
        else if (OperatingSystem.IsLinux())
        {
            Assert.Contains("linux-x64", asset.Name);
            Assert.EndsWith(".tar.gz", asset.Name);
        }
    }
}
