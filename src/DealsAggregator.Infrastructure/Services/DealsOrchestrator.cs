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

        // Fase 2 — paralela: preços + bundles ativos (ambos requerem UUID)
        ItadGamePrices?               itadData    = null;
        IReadOnlyList<ActiveBundle>   itadBundles = [];

        if (uuid is { } id)
        {
            var pricesTask  = itad.GetPricesAsync([id], country, ct);
            var bundlesTask = itad.GetGameBundlesAsync(id, country, ct);

            await Task.WhenAll(pricesTask, bundlesTask);

            itadData    = (await pricesTask).GetValueOrDefault(id);
            itadBundles = (await bundlesTask)
                .Select(b => new ActiveBundle(b.Title, b.Url, b.Store))
                .ToList();
        }

        if (ggDealsPrices is null && itadData is null)
            return null;

        return Merge(steamAppId, region, ggDealsPrices, uuid, itadData, itadBundles);
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
        // Bundles não são buscados no batch (tradeoff de performance: N chamadas para 100 jogos)
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

    public async Task<AggregatedGame?> SearchByTitleAsync(
        string title, string region = "br", CancellationToken ct = default)
    {
        // Fase 1: ITAD resolve título → UUID
        var uuid = await itad.LookupByTitleAsync(title, ct);
        if (uuid is null) return null;

        // Fase 2: ITAD resolve UUID → Steam App ID (null para jogos sem Steam)
        var steamAppId = await itad.GetSteamAppIdAsync(uuid.Value, ct);
        if (steamAppId is null) return null;

        // Fase 3: reutiliza o fluxo completo de lookup por App ID (inclui bundles)
        return await GetGameAsync(steamAppId.Value, region, ct);
    }

    public async Task<BundlePrices?> GetBundleAsync(
        int steamBundleId, string region = "br", CancellationToken ct = default)
    {
        var prices = (await ggDeals.GetBundlePricesAsync([steamBundleId], region, ct))
                         .GetValueOrDefault(steamBundleId);
        if (prices is null) return null;

        return new BundlePrices(
            SteamBundleId:      steamBundleId,
            Title:              prices.Title,
            GgDealsUrl:         prices.Url,
            CurrentRetail:      prices.CurrentRetail,
            CurrentKeyshops:    prices.CurrentKeyshops,
            HistoricalRetail:   prices.HistoricalRetail,
            HistoricalKeyshops: prices.HistoricalKeyshops,
            Currency:           prices.Currency,
            Region:             region,
            FetchedAt:          DateTimeOffset.UtcNow);
    }

    public async Task<SubPrices?> GetSubAsync(
        int steamSubId, string region = "br", CancellationToken ct = default)
    {
        var prices = (await ggDeals.GetSubPricesAsync([steamSubId], region, ct))
                         .GetValueOrDefault(steamSubId);
        if (prices is null) return null;

        return new SubPrices(
            SteamSubId:         steamSubId,
            Title:              prices.Title,
            GgDealsUrl:         prices.Url,
            CurrentRetail:      prices.CurrentRetail,
            CurrentKeyshops:    prices.CurrentKeyshops,
            HistoricalRetail:   prices.HistoricalRetail,
            HistoricalKeyshops: prices.HistoricalKeyshops,
            Currency:           prices.Currency,
            Region:             region,
            FetchedAt:          DateTimeOffset.UtcNow);
    }

    // ---------------------------------------------------------------------------

    private static AggregatedGame Merge(
        int steamAppId,
        string region,
        GgDealsPrices? ggDealsPrices,
        Guid? itadUuid,
        ItadGamePrices? itadData,
        IReadOnlyList<ActiveBundle>? bundles = null)
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
            Bundles:          bundles ?? [],
            Currency:         ggDealsPrices?.Currency ?? itadData?.HistoricalLowCurrency ?? string.Empty,
            Region:           region,
            GgDealsFetchedAt: DateTimeOffset.UtcNow,
            ItadFetchedAt:    itadUuid.HasValue ? DateTimeOffset.UtcNow : null);
    }
}
