using SteamDealX.Api.Errors;
using SteamDealX.Core.Abstractions;
using SteamDealX.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace SteamDealX.Api.Controllers;

[Tags("Games")]
[Route("[controller]")]
[EnableRateLimiting("api")]
public sealed class GamesController(IDealsOrchestrator orchestrator) : BaseApiController
{
    private const string ProblemJson = "application/problem+json";

    /// <summary>
    /// Retorna os preços atuais de todas as fontes para um único jogo, identificado pelo Steam App ID.
    /// </summary>
    /// <remarks>
    /// **Fontes de dados:**
    /// - **IsThereAnyDeal** — preços por loja (Steam, GOG, Humble, etc.) com percentual de desconto e URL de compra.
    /// - **gg.deals** — melhor preço de varejo (pode incluir lojas não cobertas pelo ITAD) e melhor preço de keyshop.
    ///
    /// **Cache:** resultados ficam em cache por 2 horas (10 min em memória, 2 h no SQLite).
    ///
    /// **Atribuição obrigatória:** exibir o campo `ggDealsUrl` como hyperlink ativo
    /// em qualquer interface que mostre os dados (exigência dos Termos de Uso do gg.deals).
    ///
    /// **Região:** código de duas letras minúsculas (ex: `br`, `us`, `eu`). Default: `br`.
    /// </remarks>
    [HttpGet("{steamAppId:int}")]
    [EndpointSummary("Preços agregados de um jogo")]
    [ProducesResponseType(typeof(AggregatedGame), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound, ProblemJson)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status429TooManyRequests, ProblemJson)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status502BadGateway, ProblemJson)]
    public async Task<IActionResult> GetGame(
        int steamAppId,
        string? region,
        CancellationToken cancellationToken)
    {
        var game = await orchestrator.GetGameAsync(steamAppId, region ?? "br", cancellationToken);
        return game is null
            ? ApiProblem(404, ErrorCodes.GameNotFound,
                "Game Not Found", $"No data found for Steam App ID {steamAppId}.")
            : Ok(game);
    }

    /// <summary>
    /// Busca dados agregados para até 100 jogos em uma única chamada.
    /// </summary>
    /// <remarks>
    /// **Parâmetro `ids`:** Steam App IDs repetindo o parâmetro ou por vírgula.
    /// Exemplo: `?ids=730&amp;ids=440&amp;ids=1245620`.
    /// Máximo de 100 IDs por requisição (limite do gg.deals).
    ///
    /// **Resposta:** dicionário `{ steamAppId: AggregatedGame | null }`.
    /// Valor `null` quando o jogo não foi encontrado em nenhuma fonte.
    ///
    /// **Cache inteligente:** IDs já em cache são servidos diretamente;
    /// apenas os IDs ausentes são buscados nas APIs upstream.
    /// </remarks>
    [HttpGet("batch")]
    [EndpointSummary("Preços de múltiplos jogos em lote")]
    [ProducesResponseType(typeof(Dictionary<int, AggregatedGame>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest, ProblemJson)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status429TooManyRequests, ProblemJson)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status502BadGateway, ProblemJson)]
    public async Task<IActionResult> GetGamesBatch(
        [FromQuery] int[]? ids,
        string? region,
        CancellationToken cancellationToken)
    {
        if (ids is null or { Length: 0 })
            return ApiProblem(400, ErrorCodes.ValidationError,
                "Validation Error", "Query parameter 'ids' is required.");

        var results = await orchestrator.GetGamesBatchAsync(
            ids.Take(100).ToArray(), region ?? "br", cancellationToken);
        return Ok(results);
    }

    /// <summary>
    /// Resolve um título de jogo via IsThereAnyDeal e retorna os preços agregados.
    /// </summary>
    /// <remarks>
    /// **Fluxo:** título → ITAD UUID (`/games/lookup/v1`) → Steam App ID (`/games/info/v2`) → preços agregados.
    ///
    /// **Atenção:** a busca é por correspondência exata de título (não é fuzzy).
    /// Erros de digitação ou variações no nome podem não retornar resultado.
    ///
    /// Jogos sem presença na Steam (exclusivos GOG, Epic, etc.) retornam 404,
    /// pois o gg.deals usa Steam App ID como identificador primário.
    ///
    /// O resultado é cacheado sob a chave do Steam App ID — lookups diretos
    /// posteriores para o mesmo jogo retornam instantaneamente do cache.
    /// </remarks>
    [HttpGet("search")]
    [EndpointSummary("Busca jogo por título")]
    [ProducesResponseType(typeof(AggregatedGame), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest, ProblemJson)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound, ProblemJson)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status429TooManyRequests, ProblemJson)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status502BadGateway, ProblemJson)]
    public async Task<IActionResult> SearchGame(
        string? title,
        string? region,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(title))
            return ApiProblem(400, ErrorCodes.ValidationError,
                "Validation Error", "Query parameter 'title' is required.");

        var game = await orchestrator.SearchByTitleAsync(title, region ?? "br", cancellationToken);
        return game is null
            ? ApiProblem(404, ErrorCodes.GameNotFound,
                "Game Not Found", $"No game matching '{title}' was found.")
            : Ok(game);
    }
}
