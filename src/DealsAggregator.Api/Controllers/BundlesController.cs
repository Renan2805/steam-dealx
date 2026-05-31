using DealsAggregator.Api.Errors;
using DealsAggregator.Core.Abstractions;
using DealsAggregator.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace DealsAggregator.Api.Controllers;

[Tags("Bundles")]
[Route("[controller]")]
[EnableRateLimiting("api")]
public sealed class BundlesController(IDealsOrchestrator orchestrator) : BaseApiController
{
    private const string ProblemJson = "application/problem+json";

    /// <summary>
    /// Retorna os preços de um bundle Steam, identificado pelo Steam Bundle ID.
    /// </summary>
    /// <remarks>
    /// Dados fornecidos exclusivamente pelo gg.deals, que rastreia preços de bundles
    /// da Steam Store. O ITAD não cobre preços de bundles.
    ///
    /// O Steam Bundle ID pode ser encontrado na URL da Steam:
    /// `store.steampowered.com/bundle/{steamBundleId}/`
    ///
    /// **Atribuição obrigatória:** exibir o campo `ggDealsUrl` como hyperlink ativo
    /// em qualquer interface que mostre os dados (exigência dos Termos de Uso do gg.deals).
    ///
    /// **Cache:** resultados ficam em cache por 2 horas.
    ///
    /// **Região:** código de duas letras minúsculas (ex: `br`, `us`, `eu`). Default: `br`.
    /// </remarks>
    [HttpGet("{steamBundleId:int}")]
    [EndpointSummary("Preços de um bundle Steam")]
    [ProducesResponseType(typeof(BundlePrices), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound, ProblemJson)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status429TooManyRequests, ProblemJson)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status502BadGateway, ProblemJson)]
    public async Task<IActionResult> GetBundle(
        int steamBundleId,
        string? region,
        CancellationToken cancellationToken)
    {
        var bundle = await orchestrator.GetBundleAsync(steamBundleId, region ?? "br", cancellationToken);
        return bundle is null
            ? ApiProblem(404, ErrorCodes.GameNotFound,
                "Bundle Not Found", $"No data found for Steam Bundle ID {steamBundleId}.")
            : Ok(bundle);
    }
}
