namespace ValheimModManager.Tests;

using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ValheimModManager.Core.Services;
using Xunit;

public class ImageCacheServiceTests : IDisposable
{
    private readonly string _tempCacheDir;

    public ImageCacheServiceTests()
    {
        _tempCacheDir = Path.Combine(Path.GetTempPath(), "VMM_ImageCacheTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempCacheDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempCacheDir))
        {
            try
            {
                Directory.Delete(_tempCacheDir, recursive: true);
            }
            catch
            {
                // ignore
            }
        }
    }

    [Fact]
    public async Task GetImageBytesAsync_InvalidUrl_ReturnsNull()
    {
        var service = new ImageCacheService(cacheDirectory: _tempCacheDir);

        Assert.Null(await service.GetImageBytesAsync(null));
        Assert.Null(await service.GetImageBytesAsync(""));
        Assert.Null(await service.GetImageBytesAsync("not-a-valid-uri"));
    }

    [Fact]
    public async Task GetImageBytesAsync_CachedOnDisk_ReturnsLocalBytesWithoutHttp()
    {
        var service = new ImageCacheService(cacheDirectory: _tempCacheDir);
        const string testUrl = "https://example.com/mod_icon.png";

        var localPath = service.GetLocalCacheFilePath(testUrl);
        var expectedBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x01, 0x02, 0x03 };
        await File.WriteAllBytesAsync(localPath, expectedBytes);

        var result = await service.GetImageBytesAsync(testUrl);

        Assert.NotNull(result);
        Assert.Equal(expectedBytes, result);
    }

    [Fact]
    public async Task GetImageBytesAsync_DownloadsAndPersistsToDisk()
    {
        var dummyBytes = new byte[] { 1, 2, 3, 4, 5 };
        var handler = new MockHttpMessageHandler(HttpStatusCode.OK, dummyBytes);
        using var client = new HttpClient(handler);

        var service = new ImageCacheService(httpClient: client, cacheDirectory: _tempCacheDir);
        const string testUrl = "https://cdn.thunderstore.io/icon.png";

        var result = await service.GetImageBytesAsync(testUrl);

        Assert.NotNull(result);
        Assert.Equal(dummyBytes, result);

        var localPath = service.GetLocalCacheFilePath(testUrl);
        Assert.True(File.Exists(localPath));
        var diskBytes = await File.ReadAllBytesAsync(localPath);
        Assert.Equal(dummyBytes, diskBytes);
    }

    private class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly byte[] _content;

        public MockHttpMessageHandler(HttpStatusCode statusCode, byte[] content)
        {
            _statusCode = statusCode;
            _content = content;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(_statusCode)
            {
                Content = new ByteArrayContent(_content)
            };
            return Task.FromResult(response);
        }
    }
}
