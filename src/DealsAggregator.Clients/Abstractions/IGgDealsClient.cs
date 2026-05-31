using DealsAggregator.Clients.Models;

namespace DealsAggregator.Clients.Abstractions;

public interface IGgDealsClient
{
    // Até 100 Steam App IDs por chamada; null para IDs desconhecidos
    Task<IReadOnlyDictionary<int, GgDealsPrices?>> GetPricesAsync(
        IReadOnlyCollection<int> steamAppIds,
        string region = "br",
        CancellationToken ct = default);

    // Até 100 Steam Bundle IDs por chamada; mesma estrutura de resposta que GetPricesAsync
    Task<IReadOnlyDictionary<int, GgDealsPrices?>> GetBundlePricesAsync(
        IReadOnlyCollection<int> steamBundleIds,
        string region = "br",
        CancellationToken ct = default);
}
