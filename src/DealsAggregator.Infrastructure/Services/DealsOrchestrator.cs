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

        // Fase 2 — única chamada: preços + historyLow embutido (spec confirma que vem junto)
        ItadGamePrices? itadData = null;

        if (uuid is { } id)
            itadData = (await itad.GetPricesAsync([id], country, ct)).GetValueOrDefault(id);

        if (ggDealsPrices is null && itadData is null)
            return null;

        return Merge(steamAppId, region, ggDealsPrices, uuid, itadData);
    }

    public async Task<IReadOnlyDictionary<int, AggregatedGame?>> GetGamesBatchAsync(
        IReadOnlyCollection<int> steamAppIds, string region = "br", CancellationToken ct = default)
    {
        if (steamAppIds.Count == 0)
            return new Dictionary<int, AggregatedGame?>();

        var country = region.ToUpperInvariant();

        // Fase 1 — paralela: gg.deals (batch único) + UUID lookups individuais no ITAD
        var ggDealsTask = ggDeals.GetPricesAsync(steamAppIds, region, ct);
        var uuidResults = await Task.WhenAll(
            steamAppIds.Select(async id => (id, uuid: await itad.LookupBySteamAppIdAsync(id, ct))));
        var ggDealsResults = await ggDealsTask;

        var idToUuid = uuidResults
            .Where(x => x.uuid.HasValue)
            .ToDictionary(x => x.id, x => x.uuid!.Value);

        // Fase 2 — única chamada batch ao ITAD (preços + historyLow embutido)
        IReadOnlyDictionary<Guid, ItadGamePrices> itadPrices = new Dictionary<Guid, ItadGamePrices>();

        var uuids = idToUuid.Values.ToList();
        if (uuids.Count > 0)
            itadPrices = await itad.GetPricesAsync(uuids, country, ct);

        return steamAppIds.ToDictionary(
            id => id,
            id =>
            {
                var prices   = ggDealsResults.GetValueOrDefault(id);
                var uuid     = idToUuid.TryGetValue(id, out var u) ? u : (Guid?)null;
                var itadData = uuid is { } du ? itadPrices.GetValueOrDefault(du) : null;

                if (prices is null && itadData is null)
                    return (AggregatedGame?)null;

                return Merge(id, region, prices, uuid, itadData);
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
        ItadGamePrices? itadData)
    {
        var offers = new List<GameOffer>((itadData?.Deals.Count ?? 0) + 2);

        // Ofertas por loja vindas do ITAD (varejo, com cut%)
        if (itadData is not null)
            foreach (var deal in itadData.Deals)
                offers.Add(new GameOffer(deal.Shop, deal.Price, deal.Regular, deal.CutPercent, deal.Url, OfferType.Retail, region));

        // Melhor preço de varejo do gg.deals — pode incluir lojas que o ITAD não cobre
        if (ggDealsPrices?.CurrentRetail is { } retail)
            offers.Add(new GameOffer("gg.deals", retail, null, 0, ggDealsPrices.Url, OfferType.Retail, region));

        // Melhor keyshop do gg.deals
        if (ggDealsPrices?.CurrentKeyshops is { } keyshop)
            offers.Add(new GameOffer("gg.deals", keyshop, null, 0, ggDealsPrices.Url, OfferType.Keyshop, region));

        // Mínimo histórico absoluto entre todas as fontes
        var historicalLow = new[]
            {
                ggDealsPrices?.HistoricalRetail,
                ggDealsPrices?.HistoricalKeyshops,
                itadData?.HistoricalLow
            }
            .Where(x => x is > 0)
            .Select(x => x!.Value)
            .DefaultIfEmpty()
            .Min() is var min && min > 0 ? min : (decimal?)null;

        return new AggregatedGame(
            SteamAppId:       steamAppId,
            Title:            ggDealsPrices?.Title ?? string.Empty,
            GgDealsUrl:       ggDealsPrices?.Url ?? string.Empty,
            ItadUuid:         itadUuid,
            Offers:           offers,
            HistoricalLow:    historicalLow,
            Bundles:          [],
            Currency:         ggDealsPrices?.Currency ?? itadData?.HistoricalLowCurrency ?? string.Empty,
            Region:           region,
            GgDealsFetchedAt: DateTimeOffset.UtcNow,
            ItadFetchedAt:    itadUuid.HasValue ? DateTimeOffset.UtcNow : null);
    }
}
