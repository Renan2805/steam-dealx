using DealsAggregator.Api.Errors;
using DealsAggregator.Core.Abstractions;
using DealsAggregator.Core.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace DealsAggregator.Api.Endpoints;

public static class GamesEndpoints
{
    public static IEndpointRouteBuilder MapGamesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/games")
            .RequireRateLimiting("api")
            .WithTags("Games");

        // ------------------------------------------------------------------ //
        // GET /games/{steamAppId}
        // ------------------------------------------------------------------ //
        group.MapGet("/{steamAppId:int}", async Task<Results<Ok<AggregatedGame>, ProblemHttpResult>> (
            int steamAppId,
            string? region,
            IDealsOrchestrator orchestrator,
            CancellationToken ct) =>
        {
            var game = await orchestrator.GetGameAsync(steamAppId, region ?? "br", ct);
            return game is null
                ? Problem(StatusCodes.Status404NotFound, ErrorCodes.GameNotFound,
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
            Uma segunda chamada para o mesmo jogo/região retorna instantaneamente do cache.

            **Atribuição obrigatória:** o campo `ggDealsUrl` deve ser exibido como hyperlink ativo
            em qualquer interface que mostre os dados (exigência dos Termos de Uso do gg.deals).

            **Região:** código de duas letras minúsculas (ex: `br`, `us`, `eu`, `de`). Default: `br`.
            Afeta os preços retornados e a moeda.
            """)
        .Produces<AggregatedGame>(200)
        .ProducesProblem(404)
        .ProducesProblem(429)
        .ProducesProblem(502);

        // ------------------------------------------------------------------ //
        // GET /games/batch?ids=730&ids=440&ids=1245620
        // ------------------------------------------------------------------ //
        group.MapGet("/batch", async Task<Results<Ok<IReadOnlyDictionary<int, AggregatedGame?>>, ProblemHttpResult>> (
            int[]? ids,
            string? region,
            IDealsOrchestrator orchestrator,
            CancellationToken ct) =>
        {
            if (ids is null or { Length: 0 })
                return Problem(StatusCodes.Status400BadRequest, ErrorCodes.ValidationError,
                    "Validation Error", "Query parameter 'ids' is required.");

            var results = await orchestrator.GetGamesBatchAsync(
                ids.Take(100).ToArray(), region ?? "br", ct);
            return TypedResults.Ok(results);
        })
        .WithName("GetGamesBatch")
        .WithSummary("Preços de múltiplos jogos em lote")
        .WithDescription("""
            Busca dados agregados para até **100 jogos** em uma única chamada.

            **Parâmetro `ids`:** Steam App IDs repetindo o parâmetro ou separados por vírgula.
            Exemplo: `?ids=730&ids=440&ids=1245620`.
            Máximo de 100 IDs por requisição (limite do gg.deals).

            **Resposta:** dicionário `{ steamAppId: AggregatedGame | null }`.
            O valor é `null` quando o jogo não foi encontrado em nenhuma das fontes.

            **Cache inteligente:** IDs já em cache são servidos diretamente;
            apenas os IDs ausentes são buscados nas APIs upstream, minimizando
            o consumo de cota do gg.deals (100 registros/min, 1000/hora).
            """)
        .Produces<IReadOnlyDictionary<int, AggregatedGame?>>(200)
        .ProducesProblem(400)
        .ProducesProblem(429)
        .ProducesProblem(502);

        // ------------------------------------------------------------------ //
        // GET /games/search?title=Elden+Ring
        // ------------------------------------------------------------------ //
        group.MapGet("/search", async Task<Results<Ok<AggregatedGame>, ProblemHttpResult>> (
            string? title,
            string? region,
            IDealsOrchestrator orchestrator,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(title))
                return Problem(StatusCodes.Status400BadRequest, ErrorCodes.ValidationError,
                    "Validation Error", "Query parameter 'title' is required.");

            var game = await orchestrator.SearchByTitleAsync(title, region ?? "br", ct);
            return game is null
                ? Problem(StatusCodes.Status404NotFound, ErrorCodes.GameNotFound,
                    "Game Not Found", $"No game matching '{title}' was found.")
                : TypedResults.Ok(game);
        })
        .WithName("SearchGame")
        .WithSummary("Busca jogo por título")
        .WithDescription("""
            Resolve um título de jogo via IsThereAnyDeal e retorna os preços agregados.

            **Atenção:** a busca é por correspondência exata de título (não é busca fuzzy).
            Erros de digitação ou variações no nome podem não retornar resultado.

            **⚠️ Não implementado:** este endpoint ainda não está funcional (retorna 501).
            """)
        .Produces<AggregatedGame>(200)
        .ProducesProblem(400)
        .ProducesProblem(404)
        .ProducesProblem(429)
        .ProducesProblem(501)
        .ProducesProblem(502);

        return app;
    }

    private static ProblemHttpResult Problem(int status, string code, string title, string detail) =>
        TypedResults.Problem(new ProblemDetails
        {
            Status     = status,
            Title      = title,
            Detail     = detail,
            Extensions = { ["code"] = code }
        });
}
