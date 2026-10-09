namespace ValheimModManager.App.Services;

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using ValheimModManager.Core.Services;

public sealed class AvaloniaImageLoader
{
    private static readonly Lazy<AvaloniaImageLoader> _instance = new(() => new AvaloniaImageLoader());
    public static AvaloniaImageLoader Instance => _instance.Value;

    private readonly ConcurrentDictionary<string, Bitmap> _memoryCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly ImageCacheService _imageCacheService;

    public AvaloniaImageLoader(ImageCacheService? imageCacheService = null)
    {
        _imageCacheService = imageCacheService ?? ImageCacheService.Instance;
    }

    public async Task<Bitmap?> LoadImageAsync(string? url, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        // 1. Controllo cache in memoria RAM (0 ms)
        if (_memoryCache.TryGetValue(url, out var cachedBitmap))
        {
            return cachedBitmap;
        }

        // 2. Controllo o download asincrono dei byte
        var bytes = await _imageCacheService.GetImageBytesAsync(url, ct);
        if (bytes == null || bytes.Length == 0)
        {
            return null;
        }

        // 3. Decodifica Bitmap in background (non blocca la UI)
        try
        {
            return await Task.Run(() =>
            {
                using var ms = new MemoryStream(bytes);
                var bitmap = new Bitmap(ms);
                _memoryCache.TryAdd(url, bitmap);
                return bitmap;
            }, ct);
        }
        catch
        {
            return null;
        }
    }
}
