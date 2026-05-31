using DealsAggregator.Core.Abstractions;

namespace DealsAggregator.Api.Endpoints;

public static class GamesEndpoints
{
    public static IEndpointRouteBuilder MapGamesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/games")
            .RequireRateLimiting("api");

        group.MapGet("/{steamAppId:int}", async (
            int steamAppId,
            string? region,
            IDealsOrchestrator orchestrator,
            CancellationToken ct) =>
        {
            var game = await orchestrator.GetGameAsync(steamAppId, region ?? "br", ct);
            return game is null ? Results.NotFound() : Results.Ok(game);
        })
        .WithName("GetGame");

        group.MapGet("/batch", async (
            int[]? ids,
            string? region,
            IDealsOrchestrator orchestrator,
            CancellationToken ct) =>
        {
            if (ids is null or { Length: 0 })
                return Results.BadRequest("ids query parameter is required");

            var results = await orchestrator.GetGamesBatchAsync(
                ids.Take(100).ToArray(), region ?? "br", ct);
            return Results.Ok(results);
        })
        .WithName("GetGamesBatch");

        group.MapGet("/search", async (
            string? title,
            string? region,
            IDealsOrchestrator orchestrator,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(title))
                return Results.BadRequest("title query parameter is required");

            var game = await orchestrator.SearchByTitleAsync(title, region ?? "br", ct);
            return game is null ? Results.NotFound() : Results.Ok(game);
        })
        .WithName("SearchGame");

        return app;
    }
}
