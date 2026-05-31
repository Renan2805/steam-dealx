namespace DealsAggregator.Core.Models;

public sealed record AggregatedGame(
    int SteamAppId,
    string Title,
    string GgDealsUrl,
    Guid? ItadUuid,
    IReadOnlyList<GameOffer> Offers,
    decimal? HistoricalLow,
    IReadOnlyList<ActiveBundle> Bundles,
    string Currency,
    string Region,
    DateTimeOffset GgDealsFetchedAt,
    DateTimeOffset? ItadFetchedAt);
