namespace ValheimModManager.Core.Services;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ValheimModManager.Core.Models;

public sealed record AppReleaseAsset(
    string Name,
    string DownloadUrl,
    long Size,
    string ContentType
);

public sealed record AppReleaseInfo(
    string TagName,
    string Version,
    string Title,
    string ReleaseNotes,
    string HtmlUrl,
    DateTimeOffset PublishedAt,
    IReadOnlyList<AppReleaseAsset> Assets
);

public sealed record AppUpdateCheckResult(
    bool HasUpdate,
    string CurrentVersion,
    AppReleaseInfo? LatestRelease,
    string? ErrorMessage = null
);

public sealed class AppUpdateService
{
    private readonly HttpClient _httpClient;
    private readonly string _repositoryOwner;
    private readonly string _repositoryName;
    private readonly ILogger<AppUpdateService> _logger;

    public AppUpdateService(
        HttpClient? httpClient = null,
        string repositoryOwner = "VasariRulez",
        string repositoryName = "ValheimModManager",
        ILogger<AppUpdateService>? logger = null)
    {
        _httpClient = httpClient ?? new HttpClient();
        _repositoryOwner = repositoryOwner;
        _repositoryName = repositoryName;
        _logger = logger ?? NullLogger<AppUpdateService>.Instance;
    }

    public async Task<AppUpdateCheckResult> CheckForUpdateAsync(string currentVersion, CancellationToken ct = default)
    {
        var cleanCurrent = CleanVersion(currentVersion);
        _logger.LogInformation("Checking for app updates. Current version: {CurrentVersion}", cleanCurrent);

        var url = $"https://api.github.com/repos/{_repositoryOwner}/{_repositoryName}/releases/latest";

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.UserAgent.Add(new ProductInfoHeaderValue("ValheimModManager", "1.0"));
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

            using var response = await _httpClient.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                var error = $"GitHub API error: {response.StatusCode} ({(int)response.StatusCode})";
                _logger.LogWarning("Failed to query GitHub Releases: {Error}", error);
                return new AppUpdateCheckResult(false, cleanCurrent, null, error);
            }

            var json = await response.Content.ReadAsStringAsync(ct);
            var release = ParseReleaseJson(json);
            if (release == null)
            {
                return new AppUpdateCheckResult(false, cleanCurrent, null, "Failed to parse GitHub release information.");
            }

            var hasUpdate = ModVersionComparer.IsNewer(release.Version, cleanCurrent);
            _logger.LogInformation("Latest release is {LatestVersion}. Has update: {HasUpdate}", release.Version, hasUpdate);

            return new AppUpdateCheckResult(hasUpdate, cleanCurrent, release);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception checking for app updates from GitHub");
            return new AppUpdateCheckResult(false, cleanCurrent, null, ex.Message);
        }
    }

    public AppReleaseAsset? SelectAssetForCurrentPlatform(AppReleaseInfo release)
    {
        if (OperatingSystem.IsWindows())
        {
            return release.Assets.FirstOrDefault(a =>
                a.Name.EndsWith("-win-x64.zip", StringComparison.OrdinalIgnoreCase) ||
                (a.Name.Contains("win", StringComparison.OrdinalIgnoreCase) && a.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)))
                ?? release.Assets.FirstOrDefault(a => a.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase));
        }
        else if (OperatingSystem.IsLinux())
        {
            return release.Assets.FirstOrDefault(a =>
                a.Name.EndsWith("-linux-x64.tar.gz", StringComparison.OrdinalIgnoreCase) ||
                (a.Name.Contains("linux", StringComparison.OrdinalIgnoreCase) && (a.Name.EndsWith(".tar.gz", StringComparison.OrdinalIgnoreCase) || a.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))))
                ?? release.Assets.FirstOrDefault(a => a.Name.EndsWith(".tar.gz", StringComparison.OrdinalIgnoreCase));
        }

        return release.Assets.FirstOrDefault();
    }

    public async Task DownloadAssetAsync(
        AppReleaseAsset asset,
        string destinationFilePath,
        IProgress<double>? progress = null,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Downloading asset {Name} to {Destination}", asset.Name, destinationFilePath);

        var dir = Path.GetDirectoryName(destinationFilePath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var tempPath = destinationFilePath + ".downloading";
        using var request = new HttpRequestMessage(HttpMethod.Get, asset.DownloadUrl);
        request.Headers.UserAgent.Add(new ProductInfoHeaderValue("ValheimModManager", "1.0"));

        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? asset.Size;
        await using (var contentStream = await response.Content.ReadAsStreamAsync(ct))
        await using (var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true))
        {
            var buffer = new byte[81920];
            long bytesReadTotal = 0;
            int bytesRead;

            while ((bytesRead = await contentStream.ReadAsync(buffer.AsMemory(0, buffer.Length), ct)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), ct);
                bytesReadTotal += bytesRead;

                if (totalBytes > 0 && progress != null)
                {
                    progress.Report((double)bytesReadTotal / totalBytes);
                }
            }
        }

        if (File.Exists(destinationFilePath))
        {
            File.Delete(destinationFilePath);
        }

        File.Move(tempPath, destinationFilePath);
        progress?.Report(1.0);
        _logger.LogInformation("Asset download finished successfully: {Path}", destinationFilePath);
    }

    public static AppReleaseInfo? ParseReleaseJson(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var tagName = root.GetProperty("tag_name").GetString() ?? "";
            var version = CleanVersion(tagName);
            var title = root.TryGetProperty("name", out var nameProp) ? (nameProp.GetString() ?? tagName) : tagName;
            var body = root.TryGetProperty("body", out var bodyProp) ? (bodyProp.GetString() ?? "") : "";
            var htmlUrl = root.TryGetProperty("html_url", out var htmlProp) ? (htmlProp.GetString() ?? "") : "";
            var publishedAt = root.TryGetProperty("published_at", out var pubProp) && pubProp.TryGetDateTimeOffset(out var dto)
                ? dto
                : DateTimeOffset.UtcNow;

            var assetsList = new List<AppReleaseAsset>();
            if (root.TryGetProperty("assets", out var assetsProp) && assetsProp.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in assetsProp.EnumerateArray())
                {
                    var aName = item.GetProperty("name").GetString() ?? "";
                    var aUrl = item.GetProperty("browser_download_url").GetString() ?? "";
                    var aSize = item.TryGetProperty("size", out var sProp) ? sProp.GetInt64() : 0L;
                    var aType = item.TryGetProperty("content_type", out var tProp) ? (tProp.GetString() ?? "") : "";

                    assetsList.Add(new AppReleaseAsset(aName, aUrl, aSize, aType));
                }
            }

            return new AppReleaseInfo(tagName, version, title, body, htmlUrl, publishedAt, assetsList);
        }
        catch
        {
            return null;
        }
    }

    public static string CleanVersion(string version)
    {
        if (string.IsNullOrWhiteSpace(version)) return "1.0.0";
        var trimmed = version.Trim();
        if (trimmed.StartsWith('v') || trimmed.StartsWith('V'))
        {
            trimmed = trimmed[1..];
        }
        return trimmed;
    }
}
