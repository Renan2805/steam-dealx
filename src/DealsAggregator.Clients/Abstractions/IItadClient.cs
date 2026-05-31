using DealsAggregator.Clients.Models;

namespace DealsAggregator.Clients.Abstractions;

public interface IItadClient
{
    Task<Guid?> LookupBySteamAppIdAsync(int steamAppId, CancellationToken ct = default);
    Task<Guid?> LookupByTitleAsync(string title, CancellationToken ct = default);

    Task<IReadOnlyList<ItadStorePrice>> GetPricesAsync(
        IReadOnlyCollection<Guid> gameIds,
        string country = "BR",
        CancellationToken ct = default);

    Task<ItadHistoryLow?> GetHistoryLowAsync(
        Guid gameId, string country = "BR", CancellationToken ct = default);
}
