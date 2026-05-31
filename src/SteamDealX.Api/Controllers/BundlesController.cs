using SteamDealX.Api.Errors;
using SteamDealX.Core.Abstractions;
using SteamDealX.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace SteamDealX.Api.Controllers;

[Tags("Bundles")]
[Route("[controller]")]
[EnableRateLimiting("api")]
public sealed class BundlesController(IDealsOrchestrator orchestrator) : BaseApiController
{
    private const string ProblemJson = "application/problem+json";

    /// <summary>
    /// Retorna dados agregados de um bundle Steam a partir de múltiplas fontes.
    /// </summary>
    /// <remarks>
    /// O Steam Bundle ID pode ser encontrado na URL da Steam:
    /// `store.steampowered.com/bundle/{steamBundleId}/`
    ///
    /// **Fontes de dados:**
    /// - **gg.deals** — melhor preço de varejo e keyshop do bundle.
    /// - **IsThereAnyDeal** — preços por loja com cut%, mínimo histórico e bundles ativos.
    ///   O ITAD resolve `bundle/{id}` para um UUID interno via `/lookup/id/shop/61/v1`.
    ///   Se o bundle não existir no ITAD, os campos ITAD ficam vazios mas os dados do gg.deals são retornados.
    ///
    /// **Atribuição obrigatória:** exibir o campo `ggDealsUrl` como hyperlink ativo
    /// em qualquer interface que mostre os dados (exigência dos Termos de Uso do gg.deals).
    ///
    /// **Cache:** resultados ficam em cache por 2 horas.
    ///
    /// **Região:** código de duas letras minúsculas (ex: `br`, `us`, `eu`). Default: `br`.
    /// </remarks>
    [HttpGet("{steamBundleId:int}")]
    [EndpointSummary("Dados agregados de um bundle Steam")]
    [ProducesResponseType(typeof(AggregatedBundle), StatusCodes.Status200OK)]
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
