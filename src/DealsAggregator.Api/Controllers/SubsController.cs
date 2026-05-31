using DealsAggregator.Api.Errors;
using DealsAggregator.Core.Abstractions;
using DealsAggregator.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace DealsAggregator.Api.Controllers;

[Tags("Subs")]
[Route("[controller]")]
[EnableRateLimiting("api")]
public sealed class SubsController(IDealsOrchestrator orchestrator) : BaseApiController
{
    private const string ProblemJson = "application/problem+json";

    /// <summary>
    /// Retorna os preços de um sub/package Steam, identificado pelo Steam Sub ID.
    /// </summary>
    /// <remarks>
    /// Subs (packages) são coleções de apps vendidas juntas na Steam Store.
    /// Exemplos: edições especiais, pacotes regionais, DLCs agrupados.
    ///
    /// O Steam Sub ID pode ser encontrado na URL da Steam:
    /// `store.steampowered.com/sub/{steamSubId}/`
    ///
    /// Dados fornecidos exclusivamente pelo gg.deals.
    ///
    /// **Atribuição obrigatória:** exibir o campo `ggDealsUrl` como hyperlink ativo
    /// em qualquer interface que mostre os dados (exigência dos Termos de Uso do gg.deals).
    ///
    /// **Cache:** resultados ficam em cache por 2 horas.
    ///
    /// **Região:** código de duas letras minúsculas (ex: `br`, `us`, `eu`). Default: `br`.
    /// </remarks>
    [HttpGet("{steamSubId:int}")]
    [EndpointSummary("Preços de um sub/package Steam")]
    [ProducesResponseType(typeof(SubPrices), StatusCodes.Status200OK)]
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
