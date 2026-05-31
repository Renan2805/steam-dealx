using System.ComponentModel;

namespace DealsAggregator.Core.Models;

/// <summary>Preços de um bundle Steam, obtidos do gg.deals.</summary>
public sealed record BundlePrices(
    [property: Description("Steam Bundle ID")]
    int SteamBundleId,

    [property: Description("Título do bundle")]
    string Title,

    [property: Description("URL da página no gg.deals — exibir como link ativo (atribuição obrigatória)")]
    string GgDealsUrl,

    [property: Description("Melhor preço atual de varejo")]
    decimal? CurrentRetail,

    [property: Description("Melhor preço atual em keyshops")]
    decimal? CurrentKeyshops,

    [property: Description("Menor preço histórico de varejo")]
    decimal? HistoricalRetail,

    [property: Description("Menor preço histórico em keyshops")]
    decimal? HistoricalKeyshops,

    [property: Description("Código de moeda ISO 4217 (ex: BRL, USD)")]
    string Currency,

    [property: Description("Região da consulta (ex: br, us)")]
    string Region,

    [property: Description("Quando os dados foram buscados")]
    DateTimeOffset FetchedAt);
