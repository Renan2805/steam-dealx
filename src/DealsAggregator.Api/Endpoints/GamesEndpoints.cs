using DealsAggregator.Api.Errors;
using DealsAggregator.Core.Abstractions;
using DealsAggregator.Core.Models;
using Microsoft.AspNetCore.Http.HttpResults;

namespace DealsAggregator.Api.Endpoints;

public static class GamesEndpoints
{
    private const string ProblemJson = "application/problem+json";

    public static IEndpointRouteBuilder MapGamesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/games")
            .RequireRateLimiting("api")
            .WithTags("Games");

        // ------------------------------------------------------------------ //
        // GET /games/{steamAppId}
        // ------------------------------------------------------------------ //
        group.MapGet("/{steamAppId:int}",
            async Task<Results<Ok<AggregatedGame>, JsonHttpResult<ApiError>>> (
                int steamAppId,
                string? region,
                IDealsOrchestrator orchestrator,
                CancellationToken ct) =>
            {
                var game = await orchestrator.GetGameAsync(steamAppId, region ?? "br", ct);
                return game is null
                    ? Problem(404, ErrorCodes.GameNotFound,
                        "Game Not Found", $"No data found for Steam App ID {steamAppId}.")
                    : TypedResults.Ok(game);
            })
        .WithName("GetGame")
        .WithSummary("Preços agregados de um jogo")
        .WithDescription("""
            Retorna os preços atuais de todas as fontes para um único jogo, identificado pelo Steam App ID.

            **Fontes de dados:**
            - **IsThereAnyDeal** — preços por loja (Steam, GOG, Humble, etc.) com percentual de desconto e URL de compra.
            - **gg.deals** — melhor preço de varejo (pode incluir lojas não cobertas pelo ITAD) e melhor preço de keyshop.

            **Cache:** resultados ficam em cache por 2 horas (10 min em memória, 2 h no SQLite).

            **Atribuição obrigatória:** exibir o campo `ggDealsUrl` como hyperlink ativo
            em qualquer interface que mostre os dados (exigência dos Termos de Uso do gg.deals).

            **Região:** código de duas letras minúsculas (ex: `br`, `us`, `eu`). Default: `br`.
            """)
        .Produces<AggregatedGame>(200)
        .Produces<ApiError>(404, ProblemJson)
        .Produces<ApiError>(429, ProblemJson)
        .Produces<ApiError>(502, ProblemJson);

        // ------------------------------------------------------------------ //
        // GET /games/batch?ids=730&ids=440&ids=1245620
        // ------------------------------------------------------------------ //
        group.MapGet("/batch",
            async Task<Results<Ok<IReadOnlyDictionary<int, AggregatedGame?>>, JsonHttpResult<ApiError>>> (
                int[]? ids,
                string? region,
                IDealsOrchestrator orchestrator,
                CancellationToken ct) =>
            {
                if (ids is null or { Length: 0 })
                    return Problem(400, ErrorCodes.ValidationError,
                        "Validation Error", "Query parameter 'ids' is required.");

                var results = await orchestrator.GetGamesBatchAsync(
                    ids.Take(100).ToArray(), region ?? "br", ct);
                return TypedResults.Ok(results);
            })
        .WithName("GetGamesBatch")
        .WithSummary("Preços de múltiplos jogos em lote")
        .WithDescription("""
            Busca dados agregados para até **100 jogos** em uma única chamada.

            **Parâmetro `ids`:** Steam App IDs repetindo o parâmetro ou por vírgula.
            Exemplo: `?ids=730&ids=440&ids=1245620`.

            **Resposta:** dicionário `{ steamAppId: AggregatedGame | null }`.
            Valor `null` quando o jogo não foi encontrado em nenhuma fonte.

            **Cache inteligente:** IDs já em cache são servidos diretamente;
            apenas os IDs ausentes são buscados nas APIs upstream.
            """)
        .Produces<IReadOnlyDictionary<int, AggregatedGame?>>(200)
        .Produces<ApiError>(400, ProblemJson)
        .Produces<ApiError>(429, ProblemJson)
        .Produces<ApiError>(502, ProblemJson);

        // ------------------------------------------------------------------ //
        // GET /games/search?title=Elden+Ring
        // ------------------------------------------------------------------ //
        group.MapGet("/search",
            async Task<Results<Ok<AggregatedGame>, JsonHttpResult<ApiError>>> (
                string? title,
                string? region,
                IDealsOrchestrator orchestrator,
                CancellationToken ct) =>
            {
                if (string.IsNullOrWhiteSpace(title))
                    return Problem(400, ErrorCodes.ValidationError,
                        "Validation Error", "Query parameter 'title' is required.");

                var game = await orchestrator.SearchByTitleAsync(title, region ?? "br", ct);
                return game is null
                    ? Problem(404, ErrorCodes.GameNotFound,
                        "Game Not Found", $"No game matching '{title}' was found.")
                    : TypedResults.Ok(game);
            })
        .WithName("SearchGame")
        .WithSummary("Busca jogo por título")
        .WithDescription("""
            Resolve um título de jogo via IsThereAnyDeal e retorna os preços agregados.

            **Atenção:** busca por correspondência exata de título (não é fuzzy).

            **⚠️ Não implementado:** retorna 501 NOT_IMPLEMENTED.
            """)
        .Produces<AggregatedGame>(200)
        .Produces<ApiError>(400, ProblemJson)
        .Produces<ApiError>(404, ProblemJson)
        .Produces<ApiError>(429, ProblemJson)
        .Produces<ApiError>(501, ProblemJson)
        .Produces<ApiError>(502, ProblemJson);

        return app;
    }

    private static JsonHttpResult<ApiError> Problem(
        int status, string code, string title, string detail) =>
        TypedResults.Json(
            new ApiError
            {
                Type     = $"https://tools.ietf.org/html/rfc9110#section-15.{status / 100}.{status % 100 / 10}",
                Title    = title,
                Status   = status,
                Detail   = detail,
                Code     = code
            },
            statusCode: status,
            contentType: "application/problem+json");
}
