namespace ValheimModManager.Core.Abstractions;

using System.Threading;
using System.Threading.Tasks;
using ValheimModManager.Core.Models;

public interface ICatalogSink
{
    Task UpsertPackageAsync(string providerId, ModSummary package, CancellationToken ct = default);
    Task CompleteProviderRefreshAsync(string providerId, int totalCount, CancellationToken ct = default);
}
