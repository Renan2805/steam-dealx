namespace SteamDealX.Clients.Models;

public sealed record GgDealsPrices(
    string Title,
    string Url,
    decimal? CurrentRetail,
    decimal? CurrentKeyshops,
    decimal? HistoricalRetail,
    decimal? HistoricalKeyshops,
    string Currency);
