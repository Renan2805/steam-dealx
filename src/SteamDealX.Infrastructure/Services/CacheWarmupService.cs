using SteamDealX.Core.Models;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SteamDealX.Infrastructure.Services;

/// <summary>
/// BackgroundService que reaquece periodicamente as entradas de cache dos jogos mais acessados,
/// evitando latência alta no primeiro request após a expiração do cache.
/// Executa a cada 90 min (antes do TTL de 2 horas) e respeita os rate limits do GgDealsClient.
/// </summary>
internal sealed class CacheWarmupService(
    IPopularGamesTracker tracker,
    HybridCache cache,
    IServiceScopeFactory scopeFactory,
    ILogger<CacheWarmupService> logger) : BackgroundService
{
    private const    int      TopGamesCount  = 50;
    private static readonly TimeSpan WarmupInterval = TimeSpan.FromMinutes(90);
    private static readonly TimeSpan StartupDelay   = TimeSpan.FromMinutes(2);

    private static readonly HybridCacheEntryOptions CacheOptions = new()
    {
        Expiration           = TimeSpan.FromHours(2),
        LocalCacheExpiration = TimeSpan.FromMinutes(10),
    };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Aguarda a inicialização completa da aplicação antes do primeiro reaquecimento
        await Task.Delay(StartupDelay, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await WarmupAsync(stoppingToken);
            await Task.Delay(WarmupInterval, stoppingToken);
        }
    }

    private async Task WarmupAsync(CancellationToken ct)
    {
        var topGames = tracker.GetTopGames(TopGamesCount);
        if (topGames.Count == 0)
        {
            logger.LogDebug("Cache warmup skipped — no popular games recorded yet.");
            return;
        }

        logger.LogInformation("Cache warmup: refreshing {Count} popular games.", topGames.Count);
        var refreshed = 0;
        var failed    = 0;

        // Usa um scope para acessar o DealsOrchestrator (scoped) a partir deste serviço singleton
        await using var scope = scopeFactory.CreateAsyncScope();
        var orchestrator = scope.ServiceProvider.GetRequiredService<DealsOrchestrator>();

        foreach (var (steamAppId, region) in topGames)
        {
            if (ct.IsCancellationRequested) break;
            try
            {
                // Busca diretamente do inner orchestrator (bypassa a camada de cache)
                // e grava o resultado fresco no HybridCache
                var game = await orchestrator.GetGameAsync(steamAppId, region, ct);
                if (game is not null)
                    await cache.SetAsync(
                        $"game:{steamAppId}:{region}", game, CacheOptions, cancellationToken: ct);
                refreshed++;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Cache warmup failed for App ID {SteamAppId}.", steamAppId);
                failed++;
            }
        }

        logger.LogInformation(
            "Cache warmup complete: {Refreshed} refreshed, {Failed} failed.", refreshed, failed);
    }
}
