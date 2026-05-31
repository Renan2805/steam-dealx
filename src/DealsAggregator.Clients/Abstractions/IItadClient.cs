using DealsAggregator.Clients.Models;

namespace DealsAggregator.Clients.Abstractions;

public interface IItadClient
{
    Task<Guid?> LookupBySteamAppIdAsync(int steamAppId, CancellationToken ct = default);
    Task<Guid?> LookupByTitleAsync(string title, CancellationToken ct = default);

    // Retorna deals agrupados por UUID — funciona para single e batch
    Task<IReadOnlyDictionary<Guid, IReadOnlyList<ItadStorePrice>>> GetPricesAsync(
        IReadOnlyCollection<Guid> gameIds,
        string country = "BR",
        CancellationToken ct = default);

    // Retorna mínimos históricos agrupados por UUID — funciona para single e batch
    Task<IReadOnlyDictionary<Guid, ItadHistoryLow?>> GetHistoryLowAsync(
        IReadOnlyCollection<Guid> gameIds,
        string country = "BR",
        CancellationToken ct = default);
}
