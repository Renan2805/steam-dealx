using System.ComponentModel;

namespace DealsAggregator.Core.Models;

/// <summary>Dados de preço agregados de um jogo a partir de múltiplas fontes.</summary>
public sealed record AggregatedGame(
    [property: Description("Steam App ID do jogo")]
    int SteamAppId,

    [property: Description("Título do jogo")]
    string Title,

    [property: Description("URL da página no gg.deals — exibir como link ativo (atribuição obrigatória pelos Termos de Uso do gg.deals)")]
    string GgDealsUrl,

    [property: Description("UUID interno do IsThereAnyDeal. Null se o jogo não foi localizado no ITAD.")]
    Guid? ItadUuid,

    [property: Description("Ofertas atuais de todas as fontes. Inclui preços por loja (ITAD), melhor varejo e melhor keyshop (gg.deals).")]
    IReadOnlyList<GameOffer> Offers,

    [property: Description("Menor preço histórico absoluto de todas as fontes (varejo + keyshop). Null se não há histórico.")]
    decimal? HistoricalLow,

    [property: Description("Bundles ativos que contêm o jogo. Ainda não implementado — sempre vazio.")]
    IReadOnlyList<ActiveBundle> Bundles,

    [property: Description("Código de moeda ISO 4217 dos preços (ex: BRL, USD, EUR)")]
    string Currency,

    [property: Description("Região da consulta (ex: br, us, eu)")]
    string Region,

    [property: Description("Timestamp da última busca no gg.deals")]
    DateTimeOffset GgDealsFetchedAt,

    [property: Description("Timestamp da última busca no ITAD. Null se o jogo não está no ITAD.")]
    DateTimeOffset? ItadFetchedAt);
