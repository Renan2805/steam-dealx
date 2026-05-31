using System.Net;
using DealsAggregator.Clients.Options;
using static Microsoft.Extensions.Options.Options;

namespace DealsAggregator.Clients.Tests;

public class GgDealsClientTests
{
    private const string BaseUrl = "https://api.gg.deals/v1/";

    [Fact]
    public async Task GetPricesAsync_KnownGame_ReturnsMappedPrices()
    {
        const string json = """
            {
              "success": true,
              "data": {
                "420": {
                  "title": "Half-Life 2: Episode Two",
                  "url": "https://gg.deals/game/half-life-2-episode-two/",
                  "prices": {
                    "currentRetail": "91.99",
                    "currentKeyshops": "24.30",
                    "historicalRetail": "2.89",
                    "historicalKeyshops": "5.61",
                    "currency": "PLN"
                  }
                }
              }
            }
            """;

        var client = CreateClient(json);
        var result = await client.GetPricesAsync([420], "pl");

        Assert.Single(result);
        var prices = result[420];
        Assert.NotNull(prices);
        Assert.Equal("Half-Life 2: Episode Two", prices.Title);
        Assert.Equal(91.99m, prices.CurrentRetail);
        Assert.Equal(24.30m, prices.CurrentKeyshops);
        Assert.Equal(2.89m, prices.HistoricalRetail);
        Assert.Equal(5.61m, prices.HistoricalKeyshops);
        Assert.Equal("PLN", prices.Currency);
    }

    [Fact]
    public async Task GetPricesAsync_UnknownGame_ReturnsNullEntry()
    {
        const string json = """{"success": true, "data": {"1": null}}""";

        var result = await CreateClient(json).GetPricesAsync([1]);

        Assert.Single(result);
        Assert.Null(result[1]);
    }

    [Fact]
    public async Task GetPricesAsync_NullableFields_ParsesCorrectly()
    {
        const string json = """
            {
              "success": true,
              "data": {
                "730": {
                  "title": "Counter-Strike 2",
                  "url": "https://gg.deals/game/counter-strike-2/",
                  "prices": {
                    "currentRetail": null,
                    "currentKeyshops": "5.00",
                    "historicalRetail": null,
                    "historicalKeyshops": "2.50",
                    "currency": "USD"
                  }
                }
              }
            }
            """;

        var result = await CreateClient(json).GetPricesAsync([730]);

        var prices = result[730];
        Assert.NotNull(prices);
        Assert.Null(prices.CurrentRetail);
        Assert.Equal(5.00m, prices.CurrentKeyshops);
    }

    [Fact]
    public async Task GetPricesAsync_EmptyIds_ReturnsEmptyDictionary()
    {
        var result = await CreateClient("{}").GetPricesAsync([]);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetPricesAsync_BuildsCorrectRequestUrl()
    {
        const string json = """{"success": true, "data": {}}""";
        var handler = new FakeHttpMessageHandler(json);
        var client = new GgDealsClient(
            new HttpClient(handler) { BaseAddress = new Uri(BaseUrl) },
            Create(new GgDealsOptions { ApiKey = "secret" }));

        await client.GetPricesAsync([420, 730], "br");

        var url = handler.LastRequest!.RequestUri!.ToString();
        Assert.Contains("ids=420,730", url);
        Assert.Contains("key=secret", url);
        Assert.Contains("region=br", url);
    }

    [Fact]
    public async Task GetBundlePricesAsync_KnownBundle_ReturnsMappedPrices()
    {
        const string json = """
            {
              "success": true,
              "data": {
                "7": {
                  "title": "Counter-Strike: Condition Zero Pack",
                  "url": "https://gg.deals/pack/counter-strike-condition-zero-pack/",
                  "prices": {
                    "currentRetail": "35.99",
                    "currentKeyshops": null,
                    "historicalRetail": "3.59",
                    "historicalKeyshops": "6.48",
                    "currency": "PLN"
                  }
                }
              }
            }
            """;

        var result = await CreateClient(json).GetBundlePricesAsync([7], "pl");

        Assert.Single(result);
        var prices = result[7];
        Assert.NotNull(prices);
        Assert.Equal("Counter-Strike: Condition Zero Pack", prices.Title);
        Assert.Equal(35.99m, prices.CurrentRetail);
        Assert.Null(prices.CurrentKeyshops);
        Assert.Equal("PLN", prices.Currency);
    }

    [Fact]
    public async Task GetBundlePricesAsync_BuildsCorrectEndpoint()
    {
        const string json = """{"success": true, "data": {}}""";
        var handler = new FakeHttpMessageHandler(json);
        var client = new GgDealsClient(
            new HttpClient(handler) { BaseAddress = new Uri(BaseUrl) },
            Create(new GgDealsOptions { ApiKey = "secret" }));

        await client.GetBundlePricesAsync([7], "br");

        var url = handler.LastRequest!.RequestUri!.ToString();
        Assert.Contains("by-steam-bundle-id", url);
        Assert.Contains("ids=7", url);
        Assert.Contains("key=secret", url);
    }

    [Fact]
    public async Task GetSubPricesAsync_KnownSub_ReturnsMappedPrices()
    {
        const string json = """
            {
              "success": true,
              "data": {
                "518699": {
                  "title": "Counter-Strike 2 - Prime Status Upgrade",
                  "url": "https://gg.deals/pack/counter-strike-2-prime-status-upgrade/",
                  "prices": {
                    "currentRetail": "13.99",
                    "currentKeyshops": "5.50",
                    "historicalRetail": "9.99",
                    "historicalKeyshops": "3.20",
                    "currency": "USD"
                  }
                }
              }
            }
            """;

        var result = await CreateClient(json).GetSubPricesAsync([518699], "us");

        Assert.Single(result);
        var prices = result[518699];
        Assert.NotNull(prices);
        Assert.Equal("Counter-Strike 2 - Prime Status Upgrade", prices.Title);
        Assert.Equal(13.99m, prices.CurrentRetail);
        Assert.Equal(5.50m, prices.CurrentKeyshops);
    }

    [Fact]
    public async Task GetSubPricesAsync_BuildsCorrectEndpoint()
    {
        const string json = """{"success": true, "data": {}}""";
        var handler = new FakeHttpMessageHandler(json);
        var client = new GgDealsClient(
            new HttpClient(handler) { BaseAddress = new Uri(BaseUrl) },
            Create(new GgDealsOptions { ApiKey = "secret" }));

        await client.GetSubPricesAsync([518699], "br");

        var url = handler.LastRequest!.RequestUri!.ToString();
        Assert.Contains("by-steam-sub-id", url);
        Assert.Contains("ids=518699", url);
        Assert.Contains("key=secret", url);
    }

    private static GgDealsClient CreateClient(string json, HttpStatusCode status = HttpStatusCode.OK) =>
        new(new HttpClient(new FakeHttpMessageHandler(json, status)) { BaseAddress = new Uri(BaseUrl) },
            Create(new GgDealsOptions { ApiKey = "test" }));
}
