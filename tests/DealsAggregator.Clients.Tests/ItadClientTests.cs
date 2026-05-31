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
    private static readonly Guid GameId4 = Guid.Parse("3d3a3c00-0000-0000-0000-000000000004");

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
        const string json = """{"found": false, "game": null}""";

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
    public async Task GetPricesAsync_ReturnsDealsByUuid()
    {
        const string json = """
            [
              {
                "id": "3d3a3c00-0000-0000-0000-000000000003",
                "deals": [
                  {
                    "shop": {"id": 1, "name": "Steam"},
                    "price": {"amount": 4.99, "amountInt": 499, "currency": "USD"},
                    "regular": {"amount": 9.99, "amountInt": 999, "currency": "USD"},
                    "cut": 50,
                    "url": "https://store.steampowered.com/app/220"
                  }
                ]
              }
            ]
            """;

        var result = await CreateClient(json).GetPricesAsync([GameId3], "US");

        Assert.Single(result);
        Assert.True(result.ContainsKey(GameId3));
        var deal = result[GameId3].Single();
        Assert.Equal("Steam", deal.Shop);
        Assert.Equal(4.99m, deal.Price);
        Assert.Equal(9.99m, deal.Regular);
        Assert.Equal(50, deal.CutPercent);
    }

    [Fact]
    public async Task GetPricesAsync_EmptyIds_ReturnsEmpty()
    {
        var result = await CreateClient("[]").GetPricesAsync([]);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetHistoryLowAsync_Found_ReturnsHistoryLowByUuid()
    {
        const string json = """
            [
              {
                "id": "3d3a3c00-0000-0000-0000-000000000004",
                "low": {
                  "shop": {"id": 1, "name": "Steam"},
                  "price": {"amount": 2.49, "amountInt": 249, "currency": "USD"},
                  "regular": {"amount": 9.99, "amountInt": 999, "currency": "USD"},
                  "cut": 75,
                  "timestamp": "2023-06-15T10:00:00Z"
                }
              }
            ]
            """;

        var result = await CreateClient(json).GetHistoryLowAsync([GameId4], "US");

        Assert.Single(result);
        var low = result[GameId4];
        Assert.NotNull(low);
        Assert.Equal(2.49m, low.Amount);
        Assert.Equal("USD", low.Currency);
        Assert.NotNull(low.Timestamp);
    }

    [Fact]
    public async Task GetHistoryLowAsync_EmptyIds_ReturnsEmpty()
    {
        var result = await CreateClient("[]").GetHistoryLowAsync([]);
        Assert.Empty(result);
    }

    private static ItadClient CreateClient(string json, HttpStatusCode status = HttpStatusCode.OK) =>
        new(new HttpClient(new FakeHttpMessageHandler(json, status)) { BaseAddress = new Uri(BaseUrl) },
            Create(new ItadOptions { ApiKey = "test" }));
}
