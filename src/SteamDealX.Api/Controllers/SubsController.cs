using SteamDealX.Api.Errors;
using SteamDealX.Core.Abstractions;
using SteamDealX.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace SteamDealX.Api.Controllers;

[Tags("Subs")]
[Route("[controller]")]
[EnableRateLimiting("api")]
public sealed class SubsController(IDealsOrchestrator orchestrator) : BaseApiController
{
    private const string ProblemJson = "application/problem+json";

    /// <summary>
    /// Retorna dados agregados de um sub/package Steam a partir de múltiplas fontes.
    /// </summary>
    /// <remarks>
    /// Subs (packages) são coleções de apps vendidas juntas na Steam Store.
    /// Exemplos: edições especiais, pacotes regionais, DLCs agrupados.
    ///
    /// O Steam Sub ID pode ser encontrado na URL da Steam:
    /// `store.steampowered.com/sub/{steamSubId}/`
    ///
    /// **Fontes de dados:**
    /// - **gg.deals** — melhor preço de varejo e keyshop do sub.
    /// - **IsThereAnyDeal** — preços por loja com cut%, mínimo histórico e bundles ativos.
    ///   O ITAD resolve `sub/{id}` para um UUID interno via `/lookup/id/shop/61/v1`.
    ///   Se o sub não existir no ITAD, os campos ITAD ficam vazios mas os dados do gg.deals são retornados.
    ///
    /// **Atribuição obrigatória:** exibir o campo `ggDealsUrl` como hyperlink ativo
    /// em qualquer interface que mostre os dados (exigência dos Termos de Uso do gg.deals).
    ///
    /// **Cache:** resultados ficam em cache por 2 horas.
    ///
    /// **Região:** código de duas letras minúsculas (ex: `br`, `us`, `eu`). Default: `br`.
    /// </remarks>
    [HttpGet("{steamSubId:int}")]
    [EndpointSummary("Dados agregados de um sub/package Steam")]
    [ProducesResponseType(typeof(AggregatedSub), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound, ProblemJson)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status429TooManyRequests, ProblemJson)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status502BadGateway, ProblemJson)]
    public async Task<IActionResult> GetSub(
        int steamSubId,
        string? region,
        CancellationToken cancellationToken)
    {
        var sub = await orchestrator.GetSubAsync(steamSubId, region ?? "br", cancellationToken);
        return sub is null
            ? ApiProblem(404, ErrorCodes.GameNotFound,
                "Sub Not Found", $"No data found for Steam Sub ID {steamSubId}.")
            : Ok(sub);
    }
}
