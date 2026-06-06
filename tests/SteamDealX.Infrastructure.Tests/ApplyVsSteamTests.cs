using SteamDealX.Core.Models;
using SteamDealX.Infrastructure.Services;

namespace SteamDealX.Infrastructure.Tests;

public class ApplyVsSteamTests
{
    private static GameOffer MakeOffer(string store, decimal price, OfferType type = OfferType.Retail) =>
        new(store, price, null, 0, "https://example.com", type, "br");

    [Fact]
    public void NoSteamOffer_AllVsSteamPercentNull()
    {
        var offers = new List<GameOffer>
        {
            MakeOffer("Fanatical", 40m),
            MakeOffer("gg.deals",  35m),
        };

        var result = DealsOrchestrator.ApplyVsSteam(offers);

        Assert.All(result, o => Assert.Null(o.VsSteamPercent));
    }

    [Fact]
    public void SteamPresent_SteamOfferHasNullVsSteamPercent()
    {
        var offers = new List<GameOffer>
        {
            MakeOffer("Steam",    50m),
            MakeOffer("Fanatical", 40m),
        };

        var result = DealsOrchestrator.ApplyVsSteam(offers);

        Assert.Null(result.Single(o => o.Store == "Steam").VsSteamPercent);
    }

    [Fact]
    public void CheaperThanSteam_NegativePercent()
    {
        var offers = new List<GameOffer>
        {
            MakeOffer("Steam",    50m),
            MakeOffer("Fanatical", 40m),
        };

        var result = DealsOrchestrator.ApplyVsSteam(offers);

        Assert.Equal(-20.0m, result.Single(o => o.Store == "Fanatical").VsSteamPercent);
    }

    [Fact]
    public void MoreExpensiveThanSteam_PositivePercent()
    {
        var offers = new List<GameOffer>
        {
            MakeOffer("Steam",    50m),
            MakeOffer("GOG",      75m),
        };

        var result = DealsOrchestrator.ApplyVsSteam(offers);

        Assert.Equal(50.0m, result.Single(o => o.Store == "GOG").VsSteamPercent);
    }

    [Fact]
    public void SteamPriceZero_AllVsSteamPercentNull()
    {
        var offers = new List<GameOffer>
        {
            MakeOffer("Steam",    0m),
            MakeOffer("Fanatical", 40m),
        };

        var result = DealsOrchestrator.ApplyVsSteam(offers);

        Assert.All(result, o => Assert.Null(o.VsSteamPercent));
    }

    [Fact]
    public void Rounding_OneDecimalPlace()
    {
        // (33.3 - 100) / 100 * 100 = -66.7
        var offers = new List<GameOffer>
        {
            MakeOffer("Steam",    100m),
            MakeOffer("Fanatical", 33.3m),
        };

        var result = DealsOrchestrator.ApplyVsSteam(offers);

        Assert.Equal(-66.7m, result.Single(o => o.Store == "Fanatical").VsSteamPercent);
    }

    [Fact]
    public void GgDealsOffer_AlsoReceivesVsSteamPercent()
    {
        var offers = new List<GameOffer>
        {
            MakeOffer("Steam",   100m),
            MakeOffer("gg.deals", 80m, OfferType.Keyshop),
        };

        var result = DealsOrchestrator.ApplyVsSteam(offers);

        Assert.Equal(-20.0m, result.Single(o => o.Store == "gg.deals").VsSteamPercent);
    }
}
