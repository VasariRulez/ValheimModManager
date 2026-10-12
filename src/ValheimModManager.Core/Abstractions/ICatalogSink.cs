namespace ValheimModManager.Core.Abstractions;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ValheimModManager.Core.Models;

public interface ICatalogSink
{
    Task UpsertPackageAsync(string providerId, ModSummary package, CancellationToken ct = default);
    Task CompleteProviderRefreshAsync(string providerId, int totalCount, CancellationToken ct = default);

    int GetProviderPackageCount(string providerId) => 0;
    IReadOnlySet<string> GetCachedChunkUrls(string providerId) => new HashSet<string>();
    Task SaveCachedChunkUrlsAsync(string providerId, IReadOnlyList<string> chunkUrls, CancellationToken ct = default) => Task.CompletedTask;
}
