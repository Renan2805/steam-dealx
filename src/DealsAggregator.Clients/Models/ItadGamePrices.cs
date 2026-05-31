namespace SteamDealX.Clients.Models;

public sealed record ItadGamePrices(
    IReadOnlyList<ItadStorePrice> Deals,
    decimal? HistoricalLow,
    string HistoricalLowCurrency);
