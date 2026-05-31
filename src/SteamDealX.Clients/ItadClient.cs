using System.Net.Http.Json;
using SteamDealX.Clients.Abstractions;
using SteamDealX.Clients.Models;
using SteamDealX.Clients.Options;
using SteamDealX.Clients.Responses;
using Microsoft.Extensions.Options;

namespace SteamDealX.Clients;

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

    public async Task<Guid?> LookupBySteamSubIdAsync(int steamSubId, CancellationToken ct = default)
    {
        // Steam shop ID on ITAD = 61 (confirmed from /games/prices/v3 responses)
        var shopProductId = $"sub/{steamSubId}";
        var resp = await httpClient.PostAsJsonAsync(
            $"lookup/id/shop/61/v1?key={options.Value.ApiKey}",
            new[] { shopProductId },
            ct);

        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync(ct);
            throw new UpstreamApiException("IsThereAnyDeal", (int)resp.StatusCode, body);
        }

        var result = await resp.Content
            .ReadFromJsonAsync<Dictionary<string, Guid?>>(cancellationToken: ct);

        return result?.GetValueOrDefault(shopProductId);
    }

    public async Task<Guid?> LookupBySteamBundleIdAsync(int steamBundleId, CancellationToken ct = default)
    {
        var shopProductId = $"bundle/{steamBundleId}";
        var resp = await httpClient.PostAsJsonAsync(
            $"lookup/id/shop/61/v1?key={options.Value.ApiKey}",
            new[] { shopProductId },
            ct);

        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync(ct);
            throw new UpstreamApiException("IsThereAnyDeal", (int)resp.StatusCode, body);
        }

        var result = await resp.Content
            .ReadFromJsonAsync<Dictionary<string, Guid?>>(cancellationToken: ct);

        return result?.GetValueOrDefault(shopProductId);
    }

    public async Task<IReadOnlyList<ItadBundle>> GetGameBundlesAsync(
        Guid itadUuid, string country = "BR", CancellationToken ct = default)
    {
        var items = await httpClient.GetFromJsonAsync<IReadOnlyList<ItadBundleEntry>>(
            $"games/bundles/v2?id={itadUuid}&country={country}&key={options.Value.ApiKey}", ct) ?? [];
        return items
            .Select(b => new ItadBundle(b.Title, b.Url, b.Page?.Name ?? string.Empty))
            .ToList();
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
