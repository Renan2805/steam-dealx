using DealsAggregator.Clients.Abstractions;
using DealsAggregator.Core.Abstractions;
using DealsAggregator.Core.Models;

namespace DealsAggregator.Infrastructure.Services;

internal sealed class DealsOrchestrator(IGgDealsClient ggDeals, IItadClient itad) : IDealsOrchestrator
{
    public Task<AggregatedGame?> GetGameAsync(
        int steamAppId, string region = "br", CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task<IReadOnlyDictionary<int, AggregatedGame?>> GetGamesBatchAsync(
        IReadOnlyCollection<int> steamAppIds, string region = "br", CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task<AggregatedGame?> SearchByTitleAsync(
        string title, string region = "br", CancellationToken ct = default)
        => throw new NotImplementedException();
}
