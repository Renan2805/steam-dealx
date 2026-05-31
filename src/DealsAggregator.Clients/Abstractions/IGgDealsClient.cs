using DealsAggregator.Clients.Models;

namespace DealsAggregator.Clients.Abstractions;

public interface IGgDealsClient
{
    // Até 100 Steam App IDs por chamada; null para IDs desconhecidos
    Task<IReadOnlyDictionary<int, GgDealsPrices?>> GetPricesAsync(
        IReadOnlyCollection<int> steamAppIds,
        string region = "br",
        CancellationToken ct = default);

    Task<IReadOnlyDictionary<int, GgDealsBundles?>> GetBundlesAsync(
        IReadOnlyCollection<int> steamAppIds,
        string region = "br",
        CancellationToken ct = default);
}
