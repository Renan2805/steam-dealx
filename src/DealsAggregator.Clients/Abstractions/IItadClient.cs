using DealsAggregator.Clients.Models;

namespace DealsAggregator.Clients.Abstractions;

public interface IItadClient
{
    Task<Guid?> LookupBySteamAppIdAsync(int steamAppId, CancellationToken ct = default);
    Task<Guid?> LookupByTitleAsync(string title, CancellationToken ct = default);

    // Retorna deals + historyLow.all por UUID — historyLow já vem embutido na resposta de /prices/v3
    Task<IReadOnlyDictionary<Guid, ItadGamePrices>> GetPricesAsync(
        IReadOnlyCollection<Guid> gameIds,
        string country = "BR",
        CancellationToken ct = default);
}
