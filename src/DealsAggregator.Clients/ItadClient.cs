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
        // GetFromJsonAsync chama EnsureSuccessStatusCode internamente;
        // erros HTTP propagam como HttpRequestException e são mapeados pelo GlobalExceptionHandler.
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

    public async Task<int?> GetSteamAppIdAsync(Guid itadUuid, CancellationToken ct = default)
    {
        var response = await httpClient.GetFromJsonAsync<ItadGameInfoResponse>(
            $"games/info/v2?id={itadUuid}&key={options.Value.ApiKey}", ct);
        return response?.Appid;
    }

    public async Task<IReadOnlyDictionary<Guid, ItadGamePrices>> GetPricesAsync(
        IReadOnlyCollection<Guid> gameIds, string country = "BR", CancellationToken ct = default)
    {
        if (gameIds.Count == 0) return new Dictionary<Guid, ItadGamePrices>();

        var resp = await httpClient.PostAsJsonAsync(
            $"games/prices/v3?country={country}&key={options.Value.ApiKey}", gameIds, ct);

        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync(ct);
            throw new UpstreamApiException("IsThereAnyDeal", (int)resp.StatusCode, body);
        }

        var items = await resp.Content.ReadFromJsonAsync<IReadOnlyList<ItadPricesResponse>>(cancellationToken: ct) ?? [];

        return items.ToDictionary(
            r => r.Id,
            r => new ItadGamePrices(
                Deals: r.Deals
                    .Select(d => new ItadStorePrice(d.Shop.Name, d.Price.Amount, d.Regular?.Amount, d.Cut, d.Url))
                    .ToList(),
                HistoricalLow: r.HistoryLow?.All?.Amount,
                HistoricalLowCurrency: r.HistoryLow?.All?.Currency ?? string.Empty));
    }
}
