using System.Net.Http.Json;
using DealsAggregator.Clients.Abstractions;
using DealsAggregator.Clients.Models;
using DealsAggregator.Clients.Options;
using DealsAggregator.Clients.Responses;
using Microsoft.Extensions.Options;

namespace DealsAggregator.Clients;

internal sealed class ItadClient(HttpClient httpClient, IOptions<ItadOptions> options) : IItadClient
{
    public async Task<Guid?> LookupBySteamAppIdAsync(int steamAppId, CancellationToken ct = default)
    {
        var response = await httpClient.GetFromJsonAsync<ItadLookupResponse>(
            $"games/lookup/v1?appid={steamAppId}&key={options.Value.ApiKey}", ct);
        return response?.Found == true ? response.Game?.Id : null;
    }

    public async Task<Guid?> LookupByTitleAsync(string title, CancellationToken ct = default)
    {
        var response = await httpClient.GetFromJsonAsync<ItadLookupResponse>(
            $"games/lookup/v1?title={Uri.EscapeDataString(title)}&key={options.Value.ApiKey}", ct);
        return response?.Found == true ? response.Game?.Id : null;
    }

    public async Task<IReadOnlyList<ItadStorePrice>> GetPricesAsync(
        IReadOnlyCollection<Guid> gameIds, string country = "BR", CancellationToken ct = default)
    {
        if (gameIds.Count == 0) return [];

        var resp = await httpClient.PostAsJsonAsync(
            $"games/prices/v3?country={country}&key={options.Value.ApiKey}", gameIds, ct);
        resp.EnsureSuccessStatusCode();

        var items = await resp.Content.ReadFromJsonAsync<IReadOnlyList<ItadPricesResponse>>(cancellationToken: ct) ?? [];

        return items
            .SelectMany(r => r.Deals.Select(d => new ItadStorePrice(
                d.Shop.Name,
                d.Price.Amount,
                d.Regular?.Amount,
                d.Cut,
                d.Url)))
            .ToList();
    }

    public async Task<ItadHistoryLow?> GetHistoryLowAsync(
        Guid gameId, string country = "BR", CancellationToken ct = default)
    {
        var resp = await httpClient.PostAsJsonAsync(
            $"games/historylow/v1?country={country}&key={options.Value.ApiKey}",
            new[] { gameId }, ct);
        resp.EnsureSuccessStatusCode();

        var items = await resp.Content.ReadFromJsonAsync<IReadOnlyList<ItadHistoryLowResponse>>(cancellationToken: ct) ?? [];
        var item = items.FirstOrDefault(r => r.Id == gameId);

        return item?.Low is null
            ? null
            : new ItadHistoryLow(item.Low.Price.Amount, item.Low.Price.Currency, item.Low.Timestamp);
    }
}
