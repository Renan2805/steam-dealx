namespace DealsAggregator.Core.Models;

public sealed record AggregatedGame(
    int SteamAppId,
    string Title,
    Guid? ItadUuid,
    IReadOnlyList<GameOffer> Offers,
    decimal? HistoricalRetailLow,
    decimal? HistoricalKeyshopLow,
    decimal? ItadHistoricalLow,
    IReadOnlyList<ActiveBundle> Bundles,
    string Currency,
    string Region,
    DateTimeOffset GgDealsFetchedAt,
    DateTimeOffset? ItadFetchedAt);
