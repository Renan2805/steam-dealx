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
    public async Task<IReadOnlyDictionary<int, GgDealsPrices?>> GetPricesAsync(
        IReadOnlyCollection<int> steamAppIds, string region = "br", CancellationToken ct = default)
    {
        if (steamAppIds.Count == 0)
            return new Dictionary<int, GgDealsPrices?>();

        var ids = string.Join(',', steamAppIds);
        var url = $"prices/by-steam-app-id/?ids={ids}&key={options.Value.ApiKey}&region={region}";

        var response = await httpClient.GetFromJsonAsync<GgDealsApiResponse>(url, ct)
            ?? throw new InvalidOperationException("Empty response from gg.deals prices endpoint.");

        if (!response.Success || response.Data is null)
            return new Dictionary<int, GgDealsPrices?>();

        return response.Data.ToDictionary(
            kvp => int.Parse(kvp.Key, CultureInfo.InvariantCulture),
            kvp => kvp.Value is null ? (GgDealsPrices?)null : MapPrices(kvp.Value));
    }

    // Bundles endpoint path TBD — will implement once confirmed with gg.deals docs
    public Task<IReadOnlyDictionary<int, GgDealsBundles?>> GetBundlesAsync(
        IReadOnlyCollection<int> steamAppIds, string region = "br", CancellationToken ct = default)
        => throw new NotImplementedException("Bundles endpoint not yet confirmed.");

    private static GgDealsPrices MapPrices(GgDealsGameData data) => new(
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
