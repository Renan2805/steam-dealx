using DealsAggregator.Api.Errors;
using DealsAggregator.Core.Abstractions;
using DealsAggregator.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace DealsAggregator.Api.Controllers;

/// <summary>
/// Endpoint unificado que aceita qualquer tipo de produto Steam diretamente
/// pelo mesmo padrão de URL da Steam Store.
/// </summary>
[Tags("Steam")]
[Route("steam")]
[EnableRateLimiting("api")]
public sealed class SteamController(IDealsOrchestrator orchestrator) : BaseApiController
{
    private const string ProblemJson = "application/problem+json";

    private static readonly HashSet<string> ValidTypes = ["app", "sub", "bundle"];

    /// <summary>
    /// Retorna dados agregados de qualquer produto Steam.
    /// </summary>
    /// <remarks>
    /// Aceita o mesmo formato de URL da Steam Store — basta copiar o trecho
    /// `/{type}/{id}` da URL e montar a chamada:
    ///
    /// | URL Steam | Chamada |
    /// |---|---|
    /// | `store.steampowered.com/app/730/` | `GET /steam/app/730` |
    /// | `store.steampowered.com/sub/518699/` | `GET /steam/sub/518699` |
    /// | `store.steampowered.com/bundle/27508/` | `GET /steam/bundle/27508` |
    ///
    /// **Tipos válidos:** `app` (jogo/DLC), `sub` (package/edição), `bundle` (bundle promocional).
    ///
    /// A resposta é sempre `AggregatedProduct` independente do tipo, com os mesmos campos
    /// — o campo `type` indica a origem. Isso permite que o client use um único handler
    /// para renderizar qualquer produto.
    ///
    /// **Fontes:** gg.deals (retail/keyshop) + IsThereAnyDeal (por loja, histórico, bundles).
    ///
    /// **Cache:** 2 horas por `(type, id, region)`.
    ///
    /// **Região:** código de duas letras minúsculas (ex: `br`, `us`). Default: `br`.
    /// </remarks>
    [HttpGet("{type}/{id:int}")]
    [EndpointSummary("Produto Steam por tipo e ID")]
    [ProducesResponseType(typeof(AggregatedProduct), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest, ProblemJson)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound, ProblemJson)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status429TooManyRequests, ProblemJson)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status502BadGateway, ProblemJson)]
    public async Task<IActionResult> Get(
        string type,
        int id,
        string? region,
        CancellationToken cancellationToken)
    {
        if (!ValidTypes.Contains(type))
            return ApiProblem(400, ErrorCodes.ValidationError,
                "Invalid Product Type",
                $"'{type}' is not a valid Steam product type. Valid values: app, sub, bundle.");

        var r = region ?? "br";

        AggregatedProduct? product = type switch
        {
            "app"    => (await orchestrator.GetGameAsync(id, r, cancellationToken))   is { } g ? ToProduct(g) : null,
            "sub"    => (await orchestrator.GetSubAsync(id, r, cancellationToken))    is { } s ? ToProduct(s) : null,
            "bundle" => (await orchestrator.GetBundleAsync(id, r, cancellationToken)) is { } b ? ToProduct(b) : null,
            _        => null
        };

        return product is null
            ? ApiProblem(404, ErrorCodes.GameNotFound,
                "Product Not Found", $"No data found for Steam {type}/{id}.")
            : Ok(product);
    }

    // ---------------------------------------------------------------------------

    private static AggregatedProduct ToProduct(AggregatedGame g) => new(
        "app", g.SteamAppId, g.Title, g.GgDealsUrl, g.ItadUuid,
        g.Offers, g.HistoricalLow, g.Bundles,
        g.Currency, g.Region, g.GgDealsFetchedAt, g.ItadFetchedAt);

    private static AggregatedProduct ToProduct(AggregatedSub s) => new(
        "sub", s.SteamSubId, s.Title, s.GgDealsUrl, s.ItadUuid,
        s.Offers, s.HistoricalLow, s.Bundles,
        s.Currency, s.Region, s.GgDealsFetchedAt, s.ItadFetchedAt);

    private static AggregatedProduct ToProduct(AggregatedBundle b) => new(
        "bundle", b.SteamBundleId, b.Title, b.GgDealsUrl, b.ItadUuid,
        b.Offers, b.HistoricalLow, b.Bundles,
        b.Currency, b.Region, b.GgDealsFetchedAt, b.ItadFetchedAt);
}
