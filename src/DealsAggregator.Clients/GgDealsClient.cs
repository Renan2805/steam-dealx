using DealsAggregator.Clients.Abstractions;
using DealsAggregator.Clients.Models;
using DealsAggregator.Clients.Options;
using Microsoft.Extensions.Options;

namespace DealsAggregator.Clients;

internal sealed class GgDealsClient(HttpClient httpClient, IOptions<GgDealsOptions> options) : IGgDealsClient
{
    public Task<IReadOnlyDictionary<int, GgDealsPrices?>> GetPricesAsync(
        IReadOnlyCollection<int> steamAppIds, string region = "br", CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task<IReadOnlyDictionary<int, GgDealsBundles?>> GetBundlesAsync(
        IReadOnlyCollection<int> steamAppIds, string region = "br", CancellationToken ct = default)
        => throw new NotImplementedException();
}
