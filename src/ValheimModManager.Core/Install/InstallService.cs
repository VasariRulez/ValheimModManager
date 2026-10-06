namespace ValheimModManager.Core.Install;

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ValheimModManager.Core.Models;

public sealed class InstallService
{
    private readonly HttpClient _httpClient;
    private readonly string _cacheDirectory;
    private readonly IReadOnlyList<IPackageLayout> _layouts;
    private readonly ILogger<InstallService> _logger;

    public InstallService(
        HttpClient httpClient,
        string? cacheDirectory = null,
        IReadOnlyList<IPackageLayout>? layouts = null,
        ILogger<InstallService>? logger = null)
    {
        _httpClient = httpClient;
        _logger = logger ?? NullLogger<InstallService>.Instance;
        _cacheDirectory = cacheDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ValheimModManager", "cache", "packages");

        _layouts = layouts ?? [new ThunderstoreLayout()];
        Directory.CreateDirectory(_cacheDirectory);
    }

    public async Task<string> DownloadPackageAsync(
        DownloadTicket ticket,
        string providerId,
        string modIdentifier,
        string version,
        IProgress<double>? progress = null,
        CancellationToken ct = default)
    {
        var modCacheDir = Path.Combine(_cacheDirectory, providerId, modIdentifier);
        Directory.CreateDirectory(modCacheDir);
        var zipPath = Path.Combine(modCacheDir, $"{version}.zip");

        if (File.Exists(zipPath) && new FileInfo(zipPath).Length > 0)
        {
            _logger.LogInformation("Using cached package zip at {Path}", zipPath);
            progress?.Report(1.0);
            return zipPath;
        }

        var tempPath = zipPath + ".downloading";
        _logger.LogInformation("Downloading package from {Uri} to {Path}", ticket.DownloadUri, zipPath);

        using var request = new HttpRequestMessage(HttpMethod.Get, ticket.DownloadUri);
        if (ticket.Headers != null)
        {
            foreach (var (k, v) in ticket.Headers)
            {
                request.Headers.TryAddWithoutValidation(k, v);
            }
        }

        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? ticket.ExpectedSize;
        await using (var remoteStream = await response.Content.ReadAsStreamAsync(ct))
        await using (var fileStream = File.Create(tempPath))
        {
            var buffer = new byte[81920];
            long bytesReadTotal = 0;
            int bytesRead;

            while ((bytesRead = await remoteStream.ReadAsync(buffer, ct)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), ct);
                bytesReadTotal += bytesRead;

                if (totalBytes > 0 && progress != null)
                {
                    progress.Report((double)bytesReadTotal / totalBytes);
                }
            }
        }

        if (File.Exists(zipPath)) File.Delete(zipPath);
        File.Move(tempPath, zipPath);
        progress?.Report(1.0);

        return zipPath;
    }

    public IReadOnlyList<string> InstallZipToProfile(string zipPath, string profileBepInExDirectory, CanonicalModId modId)
    {
        using var archive = ZipFile.OpenRead(zipPath);
        var layout = _layouts.FirstOrDefault(l => l.Matches(archive)) ?? _layouts[0];

        _logger.LogInformation("Extracting {ModId} using layout {LayoutType}", modId, layout.GetType().Name);
        var installedFiles = layout.InstallToProfile(archive, profileBepInExDirectory, modId);

        return installedFiles;
    }

    public void UninstallMod(IReadOnlyList<string> installedFiles)
    {
        foreach (var file in installedFiles)
        {
            try
            {
                var normalFile = file.EndsWith(".disabled", StringComparison.OrdinalIgnoreCase)
                    ? file
                    : file;
                var disabledFile = file.EndsWith(".disabled", StringComparison.OrdinalIgnoreCase)
                    ? file[..^".disabled".Length]
                    : file + ".disabled";

                if (File.Exists(normalFile)) File.Delete(normalFile);
                if (File.Exists(disabledFile)) File.Delete(disabledFile);

                var dir = Path.GetDirectoryName(file);
                if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir) && !Directory.EnumerateFileSystemEntries(dir).Any())
                {
                    Directory.Delete(dir, recursive: false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to remove file during uninstall: {File}", file);
            }
        }
    }

    public void ToggleMod(IReadOnlyList<string> installedFiles, bool enable)
    {
        foreach (var file in installedFiles)
        {
            try
            {
                if (enable)
                {
                    var disabledPath = file.EndsWith(".disabled", StringComparison.OrdinalIgnoreCase)
                        ? file
                        : file + ".disabled";
                    var enabledPath = file.EndsWith(".disabled", StringComparison.OrdinalIgnoreCase)
                        ? file[..^".disabled".Length]
                        : file;

                    if (File.Exists(disabledPath) && !File.Exists(enabledPath))
                    {
                        File.Move(disabledPath, enabledPath);
                    }
                }
                else
                {
                    var enabledPath = file.EndsWith(".disabled", StringComparison.OrdinalIgnoreCase)
                        ? file[..^".disabled".Length]
                        : file;
                    var disabledPath = enabledPath + ".disabled";

                    if (File.Exists(enabledPath) && !File.Exists(disabledPath))
                    {
                        File.Move(enabledPath, disabledPath);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to toggle file {File} to enabled={Enable}", file, enable);
            }
        }
    }
}
