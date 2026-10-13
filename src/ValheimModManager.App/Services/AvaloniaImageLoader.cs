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

    private const int MaxMemoryCacheItems = 1000;
    private readonly ConcurrentDictionary<string, Bitmap> _memoryCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentQueue<string> _lruOrder = new();
    private readonly SemaphoreSlim _decodeThrottle = new(4, 4);
    private readonly ImageCacheService _imageCacheService;

    public AvaloniaImageLoader(ImageCacheService? imageCacheService = null)
    {
        _imageCacheService = imageCacheService ?? ImageCacheService.Instance;
        _imageCacheService.ImageOptimizer ??= OptimizeImage;
    }

    public static byte[] OptimizeImage(byte[] rawBytes)
    {
        try
        {
            using var inMs = new MemoryStream(rawBytes);
            using var bmp = Bitmap.DecodeToWidth(inMs, 96, BitmapInterpolationMode.LowQuality);
            using var outMs = new MemoryStream();
            bmp.Save(outMs, new PngBitmapEncoderOptions());
            return outMs.ToArray();
        }
        catch
        {
            return rawBytes;
        }
    }

    public async Task<Bitmap?> LoadImageAsync(string? url, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(url) || ct.IsCancellationRequested)
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
        if (bytes == null || bytes.Length == 0 || ct.IsCancellationRequested)
        {
            return null;
        }

        // Ricontrollo cache in memoria RAM prima del semaforo
        if (_memoryCache.TryGetValue(url, out cachedBitmap))
        {
            return cachedBitmap;
        }

        // 3. Decodifica Bitmap controllata con semaforo per non saturare la CPU
        await _decodeThrottle.WaitAsync(ct);
        try
        {
            if (ct.IsCancellationRequested) return null;

            if (_memoryCache.TryGetValue(url, out var alreadyCached))
            {
                return alreadyCached;
            }

            return await Task.Run(() =>
            {
                if (ct.IsCancellationRequested) return null;

                using var ms = new MemoryStream(bytes);
                var bitmap = Bitmap.DecodeToWidth(ms, 96, BitmapInterpolationMode.LowQuality);

                if (ct.IsCancellationRequested)
                {
                    return null;
                }

                if (_memoryCache.TryAdd(url, bitmap))
                {
                    _lruOrder.Enqueue(url);
                    while (_memoryCache.Count > MaxMemoryCacheItems && _lruOrder.TryDequeue(out var oldUrl))
                    {
                        if (_memoryCache.TryRemove(oldUrl, out _))
                        {
                            break;
                        }
                    }
                    return bitmap;
                }

                return _memoryCache.TryGetValue(url, out var existing) ? existing : bitmap;
            }, ct);
        }
        catch
        {
            return null;
        }
        finally
        {
            _decodeThrottle.Release();
        }
    }

    public int ClearCache()
    {
        _memoryCache.Clear();
        _lruOrder.Clear();
        return _imageCacheService.ClearCache();
    }
}
