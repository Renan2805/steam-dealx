using System.Globalization;
using System.Net.Http.Json;
using System.Threading.RateLimiting;
using SteamDealX.Clients.Abstractions;
using SteamDealX.Clients.Models;
using SteamDealX.Clients.Options;
using SteamDealX.Clients.Responses;
using Microsoft.Extensions.Options;

namespace SteamDealX.Clients;

internal sealed class GgDealsClient(HttpClient httpClient, IOptions<GgDealsOptions> options) : IGgDealsClient
{
    // gg.deals limits: 100 records/min, 1000 records/hour.
    // Static so the budget is shared across all instances (process-wide global limiting).
    private static readonly TokenBucketRateLimiter _minuteLimiter = new(new TokenBucketRateLimiterOptions
    {
        TokenLimit           = 100,
        ReplenishmentPeriod  = TimeSpan.FromMinutes(1),
        TokensPerPeriod      = 100,
        AutoReplenishment    = true,
        QueueLimit           = 0,
        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
    });

    private static readonly TokenBucketRateLimiter _hourLimiter = new(new TokenBucketRateLimiterOptions
    {
        TokenLimit           = 1000,
        ReplenishmentPeriod  = TimeSpan.FromHours(1),
        TokensPerPeriod      = 1000,
        AutoReplenishment    = true,
        QueueLimit           = 0,
        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
    });

    public Task<IReadOnlyDictionary<int, GgDealsPrices?>> GetPricesAsync(
        IReadOnlyCollection<int> steamAppIds, string region = "br", CancellationToken ct = default) =>
        FetchAsync("prices/by-steam-app-id/", steamAppIds, region, ct);

    public Task<IReadOnlyDictionary<int, GgDealsPrices?>> GetBundlePricesAsync(
        IReadOnlyCollection<int> steamBundleIds, string region = "br", CancellationToken ct = default) =>
        FetchAsync("prices/by-steam-bundle-id/", steamBundleIds, region, ct);

    public Task<IReadOnlyDictionary<int, GgDealsPrices?>> GetSubPricesAsync(
        IReadOnlyCollection<int> steamSubIds, string region = "br", CancellationToken ct = default) =>
        FetchAsync("prices/by-steam-sub-id/", steamSubIds, region, ct);

    private async Task<IReadOnlyDictionary<int, GgDealsPrices?>> FetchAsync(
        string endpoint, IReadOnlyCollection<int> ids, string region, CancellationToken ct)
    {
        if (ids.Count == 0)
            return new Dictionary<int, GgDealsPrices?>();

        // Enforce gg.deals rate limits before sending the request.
        // Each App/Bundle ID counts as 1 record against both quotas.
        var tokenCount = ids.Count;

        using var minuteLease = _minuteLimiter.AttemptAcquire(tokenCount);
        if (!minuteLease.IsAcquired)
            throw new UpstreamApiException("gg.deals", 429,
                $"Client-side rate limit: {tokenCount} tokens would exceed the per-minute budget (100/min). Try again shortly.");

        using var hourLease = _hourLimiter.AttemptAcquire(tokenCount);
        if (!hourLease.IsAcquired)
            throw new UpstreamApiException("gg.deals", 429,
                $"Client-side rate limit: {tokenCount} tokens would exceed the per-hour budget (1000/hour). Try again in a few minutes.");

        var url = $"{endpoint}?ids={string.Join(',', ids)}&key={options.Value.ApiKey}&region={region}";

        var httpResponse = await httpClient.GetAsync(url, ct);
        if (!httpResponse.IsSuccessStatusCode)
        {
            var body = await httpResponse.Content.ReadAsStringAsync(ct);
            throw new UpstreamApiException("gg.deals", (int)httpResponse.StatusCode, body);
        }

        var response = await httpResponse.Content.ReadFromJsonAsync<GgDealsApiResponse>(ct)
            ?? throw new InvalidOperationException($"Empty response from gg.deals {endpoint}.");

        if (!response.Success || response.Data is null)
            return new Dictionary<int, GgDealsPrices?>();

        return response.Data.ToDictionary(
            kvp => int.Parse(kvp.Key, CultureInfo.InvariantCulture),
            kvp => kvp.Value is null ? (GgDealsPrices?)null : Map(kvp.Value));
    }

    private static GgDealsPrices Map(GgDealsGameData data) => new(
        data.Title,
        data.Url,
        ParseDecimal(data.Prices?.CurrentRetail),
        ParseDecimal(data.Prices?.CurrentKeyshops),
        ParseDecimal(data.Prices?.HistoricalRetail),
        ParseDecimal(data.Prices?.HistoricalKeyshops),
        data.Prices?.Currency ?? string.Empty);

    private static decimal? ParseDecimal(string? value) =>
        value is null ? null : decimal.Parse(value, CultureInfo.InvariantCulture);
}
