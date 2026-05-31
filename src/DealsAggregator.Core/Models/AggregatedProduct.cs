using System.ComponentModel;

namespace DealsAggregator.Core.Models;

/// <summary>
/// Visão unificada de qualquer produto Steam (app, sub ou bundle).
/// Retornado pelo endpoint <c>GET /steam/{type}/{id}</c>.
/// </summary>
public sealed record AggregatedProduct(
    [property: Description("Tipo do produto Steam: 'app' (jogo/DLC), 'sub' (package), 'bundle' (bundle)")]
    string Type,

    [property: Description("ID do produto na Steam (app, sub ou bundle ID conforme o type)")]
    int SteamId,

    [property: Description("Título do produto")]
    string Title,

    [property: Description("URL da página no gg.deals — exibir como link ativo (atribuição obrigatória)")]
    string GgDealsUrl,

    [property: Description("UUID interno do IsThereAnyDeal. Null se o produto não está no ITAD.")]
    Guid? ItadUuid,

    [property: Description("Ofertas atuais de todas as fontes")]
    IReadOnlyList<GameOffer> Offers,

    [property: Description("Menor preço histórico absoluto de todas as fontes. Null se sem histórico.")]
    decimal? HistoricalLow,

    [property: Description("Bundles ativos que contêm este produto (via ITAD)")]
    IReadOnlyList<ActiveBundle> Bundles,

    [property: Description("Código de moeda ISO 4217 (ex: BRL, USD)")]
    string Currency,

    [property: Description("Região da consulta (ex: br, us)")]
    string Region,

    [property: Description("Quando os dados do gg.deals foram buscados")]
    DateTimeOffset GgDealsFetchedAt,

    [property: Description("Quando os dados do ITAD foram buscados. Null se o produto não está no ITAD.")]
    DateTimeOffset? ItadFetchedAt);
