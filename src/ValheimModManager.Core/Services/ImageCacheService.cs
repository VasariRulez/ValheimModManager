namespace ValheimModManager.Core.Services;

using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

public sealed class ImageCacheService
{
    private static readonly Lazy<ImageCacheService> _instance = new(() => new ImageCacheService());
    public static ImageCacheService Instance => _instance.Value;

    private readonly HttpClient _httpClient;
    private readonly string _cacheDirectory;
    private readonly ILogger<ImageCacheService> _logger;
    private readonly SemaphoreSlim _throttle = new(6, 6);

    public ImageCacheService(
        HttpClient? httpClient = null,
        string? cacheDirectory = null,
        ILogger<ImageCacheService>? logger = null)
    {
        _httpClient = httpClient ?? new HttpClient();
        _logger = logger ?? NullLogger<ImageCacheService>.Instance;
        _cacheDirectory = cacheDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ValheimModManager", "cache", "icons");

        Directory.CreateDirectory(_cacheDirectory);
    }

    public string CacheDirectory => _cacheDirectory;

    public async Task<byte[]?> GetImageBytesAsync(string? url, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return null;
        }

        var localFile = GetLocalCacheFilePath(url);

        // 1. Lettura immediata da disco se già scaricata
        if (File.Exists(localFile))
        {
            try
            {
                return await File.ReadAllBytesAsync(localFile, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed reading cached image file: {Path}", localFile);
            }
        }

        // 2. Download asincrono controllato da semaforo
        await _throttle.WaitAsync(ct);
        try
        {
            // Ricontrollo in caso un altro task l'abbia scaricata nel frattempo
            if (File.Exists(localFile))
            {
                return await File.ReadAllBytesAsync(localFile, ct);
            }

            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            request.Headers.UserAgent.ParseAdd("ValheimModManager/1.0");

            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead, ct);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var bytes = await response.Content.ReadAsByteArrayAsync(ct);
            if (bytes == null || bytes.Length == 0)
            {
                return null;
            }

            // Scrittura asincrona nella cache su disco
            try
            {
                var tempFile = localFile + "." + Guid.NewGuid().ToString("N") + ".tmp";
                await File.WriteAllBytesAsync(tempFile, bytes, ct);
                File.Move(tempFile, localFile, overwrite: true);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed writing image to cache file: {Path}", localFile);
            }

            return bytes;
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed downloading image from {Url}", url);
            return null;
        }
        finally
        {
            _throttle.Release();
        }
    }

    public string GetLocalCacheFilePath(string url)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(url))).ToLowerInvariant();
        return Path.Combine(_cacheDirectory, $"{hash}.png");
    }
}
