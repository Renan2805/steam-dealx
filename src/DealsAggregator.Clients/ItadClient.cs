using DealsAggregator.Clients.Abstractions;
using DealsAggregator.Clients.Models;
using DealsAggregator.Clients.Options;
using Microsoft.Extensions.Options;

namespace DealsAggregator.Clients;

internal sealed class ItadClient(HttpClient httpClient, IOptions<ItadOptions> options) : IItadClient
{
    public Task<Guid?> LookupBySteamAppIdAsync(int steamAppId, CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task<Guid?> LookupByTitleAsync(string title, CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task<IReadOnlyList<ItadStorePrice>> GetPricesAsync(
        IReadOnlyCollection<Guid> gameIds, string country = "BR", CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task<ItadHistoryLow?> GetHistoryLowAsync(
        Guid gameId, string country = "BR", CancellationToken ct = default)
        => throw new NotImplementedException();
}
