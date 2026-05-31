using System.Net;
using DealsAggregator.Clients.Options;
using static Microsoft.Extensions.Options.Options;

namespace DealsAggregator.Clients.Tests;

public class ItadClientTests
{
    private const string BaseUrl = "https://api.isthereanydeal.com/";

    private static readonly Guid GameId1 = Guid.Parse("3d3a3c00-0000-0000-0000-000000000001");
    private static readonly Guid GameId2 = Guid.Parse("3d3a3c00-0000-0000-0000-000000000002");
    private static readonly Guid GameId3 = Guid.Parse("3d3a3c00-0000-0000-0000-000000000003");

    [Fact]
    public async Task LookupBySteamAppIdAsync_GameFound_ReturnsUuid()
    {
        const string json = """
            {
              "found": true,
              "game": {
                "id": "3d3a3c00-0000-0000-0000-000000000001",
                "slug": "half-life-2",
                "title": "Half-Life 2"
              }
            }
            """;

        var result = await CreateClient(json).LookupBySteamAppIdAsync(220);

        Assert.Equal(GameId1, result);
    }

    [Fact]
    public async Task LookupBySteamAppIdAsync_GameNotFound_ReturnsNull()
    {
        // Spec: when found:false, "game" key may be absent entirely
        const string json = """{"found": false}""";

        var result = await CreateClient(json).LookupBySteamAppIdAsync(99999);

        Assert.Null(result);
    }

    [Fact]
    public async Task LookupByTitleAsync_GameFound_ReturnsUuid()
    {
        const string json = """
            {
              "found": true,
              "game": {
                "id": "3d3a3c00-0000-0000-0000-000000000002",
                "slug": "portal-2",
                "title": "Portal 2"
              }
            }
            """;

        var result = await CreateClient(json).LookupByTitleAsync("Portal 2");

        Assert.Equal(GameId2, result);
    }

    [Fact]
    public async Task GetPricesAsync_ReturnsDealsByUuidWithHistoricalLow()
    {
        const string json = """
            [
              {
                "id": "3d3a3c00-0000-0000-0000-000000000003",
                "historyLow": {
                  "all": {"amount": 0.99, "amountInt": 99, "currency": "USD"},
                  "y1":  {"amount": 0.99, "amountInt": 99, "currency": "USD"},
                  "m3":  {"amount": 9.99, "amountInt": 999, "currency": "USD"}
                },
                "deals": [
                  {
                    "shop": {"id": 61, "name": "Steam"},
                    "price": {"amount": 4.99, "amountInt": 499, "currency": "USD"},
                    "regular": {"amount": 9.99, "amountInt": 999, "currency": "USD"},
                    "cut": 50,
                    "voucher": null,
                    "storeLow": null,
                    "flag": null,
                    "drm": [],
                    "platforms": [],
                    "timestamp": "2024-01-01T00:00:00Z",
                    "expiry": null,
                    "url": "https://store.steampowered.com/app/220"
                  }
                ]
              }
            ]
            """;

        var result = await CreateClient(json).GetPricesAsync([GameId3], "US");

        Assert.Single(result);
        Assert.True(result.ContainsKey(GameId3));

        var data = result[GameId3];
        var deal = data.Deals.Single();
        Assert.Equal("Steam", deal.Shop);
        Assert.Equal(4.99m, deal.Price);
        Assert.Equal(9.99m, deal.Regular);
        Assert.Equal(50, deal.CutPercent);

        // historyLow.all embutido na mesma resposta
        Assert.Equal(0.99m, data.HistoricalLow);
        Assert.Equal("USD", data.HistoricalLowCurrency);
    }

    [Fact]
    public async Task GetPricesAsync_NullHistoryLow_ReturnedAsNull()
    {
        const string json = """
            [
              {
                "id": "3d3a3c00-0000-0000-0000-000000000003",
                "historyLow": {"all": null, "y1": null, "m3": null},
                "deals": []
              }
            ]
            """;

        var result = await CreateClient(json).GetPricesAsync([GameId3], "US");

        Assert.Null(result[GameId3].HistoricalLow);
    }

    [Fact]
    public async Task GetPricesAsync_EmptyIds_ReturnsEmpty()
    {
        var result = await CreateClient("[]").GetPricesAsync([]);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetSteamAppIdAsync_SteamGame_ReturnsSteamAppId()
    {
        // /games/info/v2 retorna appid quando o jogo está na Steam
        const string json = """{"appid": 220}""";

        var result = await CreateClient(json).GetSteamAppIdAsync(GameId1);

        Assert.Equal(220, result);
    }

    [Fact]
    public async Task GetSteamAppIdAsync_NonSteamGame_ReturnsNull()
    {
        // Jogos exclusivos GOG/Epic têm appid: null no ITAD
        const string json = """{"appid": null}""";

        var result = await CreateClient(json).GetSteamAppIdAsync(GameId1);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetGameBundlesAsync_GameInBundles_ReturnsMappedBundles()
    {
        const string json = """
            [
              {
                "id": 1234,
                "title": "Humble Monthly Bundle",
                "page": {"id": 8, "name": "Humble Bundle", "shopId": 6},
                "url": "https://www.humblebundle.com/monthly",
                "isMature": false,
                "publish": "2024-01-01T00:00:00Z",
                "expiry": "2024-01-31T00:00:00Z",
                "note": null,
                "counts": {"games": 10, "media": 0},
                "tiers": []
              }
            ]
            """;

        var result = await CreateClient(json).GetGameBundlesAsync(GameId1, "US");

        Assert.Single(result);
        Assert.Equal("Humble Monthly Bundle", result[0].Title);
        Assert.Equal("https://www.humblebundle.com/monthly", result[0].Url);
        Assert.Equal("Humble Bundle", result[0].Store);
    }

    [Fact]
    public async Task GetGameBundlesAsync_NoBundles_ReturnsEmpty()
    {
        var result = await CreateClient("[]").GetGameBundlesAsync(GameId1, "US");
        Assert.Empty(result);
    }

    private static ItadClient CreateClient(string json, HttpStatusCode status = HttpStatusCode.OK) =>
        new(new HttpClient(new FakeHttpMessageHandler(json, status)) { BaseAddress = new Uri(BaseUrl) },
            Create(new ItadOptions { ApiKey = "test" }));
}
