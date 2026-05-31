using DealsAggregator.Core.Abstractions;
using DealsAggregator.Core.Models;
using Microsoft.AspNetCore.Http.HttpResults;

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
        group.MapGet("/{steamAppId:int}", async Task<Results<Ok<AggregatedGame>, NotFound>> (
            int steamAppId,
            string? region,
            IDealsOrchestrator orchestrator,
            CancellationToken ct) =>
        {
            var game = await orchestrator.GetGameAsync(steamAppId, region ?? "br", ct);
            return game is null ? TypedResults.NotFound() : TypedResults.Ok(game);
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
            """);

        // ------------------------------------------------------------------ //
        // GET /games/batch?ids=730,440,1245620
        // ------------------------------------------------------------------ //
        group.MapGet("/batch", async Task<Results<Ok<IReadOnlyDictionary<int, AggregatedGame?>>, BadRequest<string>>> (
            int[]? ids,
            string? region,
            IDealsOrchestrator orchestrator,
            CancellationToken ct) =>
        {
            if (ids is null or { Length: 0 })
                return TypedResults.BadRequest("ids query parameter is required");

            var results = await orchestrator.GetGamesBatchAsync(
                ids.Take(100).ToArray(), region ?? "br", ct);
            return TypedResults.Ok(results);
        })
        .WithName("GetGamesBatch")
        .WithSummary("Preços de múltiplos jogos em lote")
        .WithDescription("""
            Busca dados agregados para até **100 jogos** em uma única chamada.

            **Parâmetro `ids`:** Steam App IDs separados por `&ids=` repetido ou vírgula.
            Exemplo: `?ids=730&ids=440&ids=1245620` ou `?ids=730,440,1245620`.
            Máximo de 100 IDs por requisição (limite imposto pelo gg.deals).

            **Resposta:** dicionário `{ steamAppId: AggregatedGame | null }`.
            O valor é `null` quando o jogo não foi encontrado em nenhuma das fontes.

            **Cache inteligente:** IDs já em cache são servidos diretamente;
            apenas os IDs ausentes são buscados nas APIs upstream. Isso minimiza
            o consumo de cota do gg.deals (100 registros/min, 1000/hora).

            **Uso típico:** página de wishlist ou lista de jogos da biblioteca Steam,
            onde múltiplos App IDs precisam ser comparados de uma vez.
            """);

        // ------------------------------------------------------------------ //
        // GET /games/search?title=Elden+Ring
        // ------------------------------------------------------------------ //
        group.MapGet("/search", async Task<Results<Ok<AggregatedGame>, NotFound, BadRequest<string>>> (
            string? title,
            string? region,
            IDealsOrchestrator orchestrator,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(title))
                return TypedResults.BadRequest("title query parameter is required");

            var game = await orchestrator.SearchByTitleAsync(title, region ?? "br", ct);
            return game is null ? TypedResults.NotFound() : TypedResults.Ok(game);
        })
        .WithName("SearchGame")
        .WithSummary("Busca jogo por título")
        .WithDescription("""
            Resolve um título de jogo via IsThereAnyDeal e retorna os preços agregados.

            **Atenção:** a busca do ITAD é por correspondência exata de título (não é busca fuzzy).
            Erros de digitação ou variações no nome podem não retornar resultado.
            Para títulos com espaços, encode como `+` ou `%20`.

            Exemplo: `?title=Elden+Ring`, `?title=Counter-Strike+2`.

            **⚠️ Não implementado:** este endpoint ainda não está funcional.
            A resolução de título → Steam App ID requer um passo adicional no ITAD
            que ainda não foi implementado. Retorna `501 Not Implemented`.
            """);

        return app;
    }
}
