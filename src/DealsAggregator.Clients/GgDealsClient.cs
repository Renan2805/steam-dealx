using System.Globalization;
using System.Net.Http.Json;
using DealsAggregator.Clients.Abstractions;
using DealsAggregator.Clients.Models;
using DealsAggregator.Clients.Options;
using DealsAggregator.Clients.Responses;
using Microsoft.Extensions.Options;

namespace DealsAggregator.Clients;

internal sealed class GgDealsClient(HttpClient httpClient, IOptions<GgDealsOptions> options) : IGgDealsClient
{
    public Task<IReadOnlyDictionary<int, GgDealsPrices?>> GetPricesAsync(
        IReadOnlyCollection<int> steamAppIds, string region = "br", CancellationToken ct = default) =>
        FetchAsync("prices/by-steam-app-id/", steamAppIds, region, ct);

    public Task<IReadOnlyDictionary<int, GgDealsPrices?>> GetBundlePricesAsync(
        IReadOnlyCollection<int> steamBundleIds, string region = "br", CancellationToken ct = default) =>
        FetchAsync("prices/by-steam-bundle-id/", steamBundleIds, region, ct);

    private async Task<IReadOnlyDictionary<int, GgDealsPrices?>> FetchAsync(
        string endpoint, IReadOnlyCollection<int> ids, string region, CancellationToken ct)
    {
        if (ids.Count == 0)
            return new Dictionary<int, GgDealsPrices?>();

        var url = $"{endpoint}?ids={string.Join(',', ids)}&key={options.Value.ApiKey}&region={region}";

        var response = await httpClient.GetFromJsonAsync<GgDealsApiResponse>(url, ct)
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
