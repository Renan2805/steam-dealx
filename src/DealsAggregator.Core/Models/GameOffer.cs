using System.ComponentModel;

namespace DealsAggregator.Core.Models;

/// <summary>Tipo de oferta.</summary>
public enum OfferType
{
    /// <summary>Loja oficial (Steam, GOG, Humble, etc.).</summary>
    Retail,
    /// <summary>Revendedor de chaves.</summary>
    Keyshop
}

/// <summary>Oferta de preço em uma loja específica.</summary>
public sealed record GameOffer(
    [property: Description("Nome da loja. 'gg.deals' indica o melhor preço agregado deles (pode representar uma loja não coberta pelo ITAD).")]
    string Store,

    [property: Description("Preço atual na moeda da região solicitada")]
    decimal Price,

    [property: Description("Preço sem desconto. Null para ofertas originadas do gg.deals.")]
    decimal? Regular,

    [property: Description("Desconto em % (0–100). 0 para ofertas do gg.deals.")]
    int CutPercent,

    [property: Description("URL da oferta. As tags de afiliado do ITAD não podem ser removidas (exigência dos Termos de Uso do ITAD).")]
    string Url,

    [property: Description("Tipo: Retail (loja oficial) ou Keyshop")]
    OfferType Type,

    [property: Description("Região desta oferta")]
    string Region);
