using DealsAggregator.Clients.Abstractions;
using DealsAggregator.Clients.Models;
using DealsAggregator.Core.Abstractions;
using DealsAggregator.Core.Models;

namespace DealsAggregator.Infrastructure.Services;

internal sealed class DealsOrchestrator(IGgDealsClient ggDeals, IItadClient itad) : IDealsOrchestrator
{
    public async Task<AggregatedGame?> GetGameAsync(
        int steamAppId, string region = "br", CancellationToken ct = default)
    {
        var country = region.ToUpperInvariant();

        // Fase 1 — paralela: gg.deals + lookup de UUID no ITAD
        var ggDealsTask = ggDeals.GetPricesAsync([steamAppId], region, ct);
        var uuidTask    = itad.LookupBySteamAppIdAsync(steamAppId, ct);

        await Task.WhenAll(ggDealsTask, uuidTask);

        var ggDealsPrices = (await ggDealsTask).GetValueOrDefault(steamAppId);
        var uuid          = await uuidTask;

        // Fase 2 — paralela: preços e mínimo histórico do ITAD (só se UUID encontrado)
        IReadOnlyDictionary<Guid, IReadOnlyList<ItadStorePrice>> itadPrices = new Dictionary<Guid, IReadOnlyList<ItadStorePrice>>();
        IReadOnlyDictionary<Guid, ItadHistoryLow?> itadLows                = new Dictionary<Guid, ItadHistoryLow?>();

        if (uuid is { } id)
        {
            var pricesTask = itad.GetPricesAsync([id], country, ct);
            var histTask   = itad.GetHistoryLowAsync([id], country, ct);

            await Task.WhenAll(pricesTask, histTask);

            itadPrices = await pricesTask;
            itadLows   = await histTask;
        }

        if (ggDealsPrices is null && itadPrices.Count == 0)
            return null;

        return Merge(steamAppId, region, ggDealsPrices, uuid,
            uuid is { } u ? itadPrices.GetValueOrDefault(u) ?? [] : [],
            uuid is { } v ? itadLows.GetValueOrDefault(v)          : null);
    }

    public async Task<IReadOnlyDictionary<int, AggregatedGame?>> GetGamesBatchAsync(
        IReadOnlyCollection<int> steamAppIds, string region = "br", CancellationToken ct = default)
    {
        if (steamAppIds.Count == 0)
            return new Dictionary<int, AggregatedGame?>();

        var country = region.ToUpperInvariant();

        // Fase 1 — paralela: gg.deals (batch único) + UUID lookups individuais no ITAD
        var ggDealsTask  = ggDeals.GetPricesAsync(steamAppIds, region, ct);
        var uuidResults  = await Task.WhenAll(
            steamAppIds.Select(async id => (id, uuid: await itad.LookupBySteamAppIdAsync(id, ct))));
        var ggDealsResults = await ggDealsTask;

        var idToUuid = uuidResults
            .Where(x => x.uuid.HasValue)
            .ToDictionary(x => x.id, x => x.uuid!.Value);

        var uuids = idToUuid.Values.ToList();

        // Fase 2 — duas chamadas batch ao ITAD
        IReadOnlyDictionary<Guid, IReadOnlyList<ItadStorePrice>> itadPrices = new Dictionary<Guid, IReadOnlyList<ItadStorePrice>>();
        IReadOnlyDictionary<Guid, ItadHistoryLow?> itadLows                = new Dictionary<Guid, ItadHistoryLow?>();

        if (uuids.Count > 0)
        {
            var pricesTask = itad.GetPricesAsync(uuids, country, ct);
            var histTask   = itad.GetHistoryLowAsync(uuids, country, ct);

            await Task.WhenAll(pricesTask, histTask);

            itadPrices = await pricesTask;
            itadLows   = await histTask;
        }

        return steamAppIds.ToDictionary(
            id => id,
            id =>
            {
                var prices = ggDealsResults.GetValueOrDefault(id);
                var uuid   = idToUuid.TryGetValue(id, out var u) ? u : (Guid?)null;
                var deals  = uuid is { } du ? itadPrices.GetValueOrDefault(du) ?? [] : (IReadOnlyList<ItadStorePrice>)[];
                var low    = uuid is { } lu ? itadLows.GetValueOrDefault(lu)         : null;

                if (prices is null && deals.Count == 0)
                    return (AggregatedGame?)null;

                return Merge(id, region, prices, uuid, deals, low);
            });
    }

    public Task<AggregatedGame?> SearchByTitleAsync(
        string title, string region = "br", CancellationToken ct = default)
        => throw new NotImplementedException(
            "Title search requires resolving ITAD UUID back to Steam App ID — not yet implemented.");

    // ---------------------------------------------------------------------------

    private static AggregatedGame Merge(
        int steamAppId,
        string region,
        GgDealsPrices? ggDealsPrices,
        Guid? itadUuid,
        IReadOnlyList<ItadStorePrice> itadDeals,
        ItadHistoryLow? itadLow)
    {
        var offers = new List<GameOffer>(itadDeals.Count + 2);

        // Ofertas por loja vindas do ITAD (varejo, com cut%)
        foreach (var deal in itadDeals)
            offers.Add(new GameOffer(deal.Shop, deal.Price, deal.Regular, deal.CutPercent, deal.Url, OfferType.Retail, region));

        // Melhor preço de varejo do gg.deals — pode incluir lojas que o ITAD não cobre
        if (ggDealsPrices?.CurrentRetail is { } retail)
            offers.Add(new GameOffer("gg.deals", retail, null, 0, ggDealsPrices.Url, OfferType.Retail, region));

        // Melhor keyshop do gg.deals
        if (ggDealsPrices?.CurrentKeyshops is { } keyshop)
            offers.Add(new GameOffer("gg.deals", keyshop, null, 0, ggDealsPrices.Url, OfferType.Keyshop, region));

        // Mínimo histórico absoluto entre todas as fontes
        var historicalLow = new[] { ggDealsPrices?.HistoricalRetail, ggDealsPrices?.HistoricalKeyshops, itadLow?.Amount }
            .Where(x => x.HasValue)
            .Select(x => x!.Value)
            .DefaultIfEmpty()
            .Min() is var min && min > 0 ? min : (decimal?)null;

        return new AggregatedGame(
            SteamAppId:      steamAppId,
            Title:           ggDealsPrices?.Title ?? string.Empty,
            GgDealsUrl:      ggDealsPrices?.Url ?? string.Empty,
            ItadUuid:        itadUuid,
            Offers:          offers,
            HistoricalLow:   historicalLow,
            Bundles:         [],
            Currency:        ggDealsPrices?.Currency ?? itadLow?.Currency ?? string.Empty,
            Region:          region,
            GgDealsFetchedAt: DateTimeOffset.UtcNow,
            ItadFetchedAt:   itadUuid.HasValue ? DateTimeOffset.UtcNow : null);
    }
}
