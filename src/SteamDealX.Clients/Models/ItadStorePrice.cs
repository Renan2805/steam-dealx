namespace SteamDealX.Clients.Models;

public sealed record ItadStorePrice(
    string Shop,
    decimal Price,
    decimal? Regular,
    int CutPercent,
    string Url);
