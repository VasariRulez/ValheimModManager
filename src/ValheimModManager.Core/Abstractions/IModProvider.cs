namespace ValheimModManager.Core.Abstractions;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ValheimModManager.Core.Models;

[Flags]
public enum ProviderCapabilities
{
    None = 0,
    FullIndex = 1,
    RemoteSearch = 2,
    RequiresAuth = 4
}

public interface IModProvider
{
    string Id { get; }
    string DisplayName { get; }
    ProviderCapabilities Capabilities { get; }

    Task RefreshCatalogAsync(ICatalogSink sink, CancellationToken ct = default);
    IAsyncEnumerable<ModSummary> SearchAsync(ModQuery query, CancellationToken ct = default);
    Task<ModDetails?> GetDetailsAsync(ModKey key, CancellationToken ct = default);
    Task<DownloadTicket> ResolveDownloadAsync(ModVersionRef versionRef, DownloadContext context, CancellationToken ct = default);
}
