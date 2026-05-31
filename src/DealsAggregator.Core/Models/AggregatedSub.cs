using System.ComponentModel;

namespace SteamDealX.Core.Models;

/// <summary>Dados de preço agregados de um sub/package Steam a partir de múltiplas fontes.</summary>
public sealed record AggregatedSub(
    [property: Description("Steam Sub ID (store.steampowered.com/sub/{id}/)")]
    int SteamSubId,

    [property: Description("Título do sub/package")]
    string Title,

    [property: Description("URL da página no gg.deals — exibir como link ativo (atribuição obrigatória)")]
    string GgDealsUrl,

    [property: Description("UUID interno do IsThereAnyDeal. Null se o sub não está no ITAD.")]
    Guid? ItadUuid,

    [property: Description("Ofertas atuais de todas as fontes (ITAD por loja + gg.deals melhor retail/keyshop)")]
    IReadOnlyList<GameOffer> Offers,

    [property: Description("Menor preço histórico absoluto de todas as fontes (varejo + keyshop). Null se sem histórico.")]
    decimal? HistoricalLow,

    [property: Description("Bundles ativos que contêm este sub (via ITAD)")]
    IReadOnlyList<ActiveBundle> Bundles,

    [property: Description("Código de moeda ISO 4217 dos preços (ex: BRL, USD)")]
    string Currency,

    [property: Description("Região da consulta (ex: br, us)")]
    string Region,

    [property: Description("Quando os dados do gg.deals foram buscados")]
    DateTimeOffset GgDealsFetchedAt,

    [property: Description("Quando os dados do ITAD foram buscados. Null se o sub não está no ITAD.")]
    DateTimeOffset? ItadFetchedAt);
