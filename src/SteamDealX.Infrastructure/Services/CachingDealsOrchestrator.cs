using SteamDealX.Core.Abstractions;
using SteamDealX.Core.Models;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace SteamDealX.Infrastructure.Services;

internal sealed class CachingDealsOrchestrator(
    DealsOrchestrator inner,
    HybridCache cache,
    IPopularGamesTracker tracker,
    ILogger<CachingDealsOrchestrator> logger) : IDealsOrchestrator
{
    private static readonly HybridCacheEntryOptions GameOptions = new()
    {
        Expiration           = TimeSpan.FromHours(2),
        LocalCacheExpiration = TimeSpan.FromMinutes(10),
    };

    public async Task<AggregatedGame?> GetGameAsync(
        int steamAppId, string region = "br", CancellationToken ct = default)
    {
        var cacheKey = $"game:{steamAppId}:{region}";
        var result = await cache.GetOrCreateAsync(
            cacheKey,
            async ct =>
            {
                logger.LogDebug("Cache miss: {CacheKey}", cacheKey);
                return await inner.GetGameAsync(steamAppId, region, ct);
            },
            GameOptions,
            cancellationToken: ct);

        if (result is null)
            await cache.RemoveAsync(cacheKey, ct);
        else
            tracker.RecordAccess(steamAppId, region);

        return result;
    }

    public async Task<IReadOnlyDictionary<int, AggregatedGame?>> GetGamesBatchAsync(
        IReadOnlyCollection<int> steamAppIds, string region = "br", CancellationToken ct = default)
    {
        var cached  = new Dictionary<int, AggregatedGame?>();
        var missing = new List<int>();

        foreach (var id in steamAppIds)
        {
            var hit = await cache.GetOrCreateAsync<AggregatedGame?>(
                $"game:{id}:{region}",
                _ => ValueTask.FromResult<AggregatedGame?>(null),
                GameOptions,
                cancellationToken: ct);

            if (hit is not null)
            {
                cached[id] = hit;
                tracker.RecordAccess(id, region);
            }
            else
            {
                missing.Add(id);
            }
        }

        if (missing.Count == 0)
        {
            logger.LogDebug("Batch {Total} games: all from cache (region={Region})", steamAppIds.Count, region);
            return cached;
        }

        var fetched = await inner.GetGamesBatchAsync(missing, region, ct);

        foreach (var (id, game) in fetched)
        {
            if (game is not null)
            {
                await cache.SetAsync($"game:{id}:{region}", game, GameOptions, cancellationToken: ct);
                tracker.RecordAccess(id, region);
            }
            cached[id] = game;
        }

        logger.LogInformation(
            "Batch {Total} games: {HitCount} from cache, {MissCount} fetched upstream (region={Region})",
            steamAppIds.Count, cached.Count - missing.Count, missing.Count, region);

        return cached;
    }

    public async Task<AggregatedBundle?> GetBundleAsync(
        int steamBundleId, string region = "br", CancellationToken ct = default)
    {
        var cacheKey = $"bundle:{steamBundleId}:{region}";
        var result = await cache.GetOrCreateAsync(
            cacheKey,
            async ct =>
            {
                logger.LogDebug("Cache miss: {CacheKey}", cacheKey);
                return await inner.GetBundleAsync(steamBundleId, region, ct);
            },
            GameOptions,
            cancellationToken: ct);

        if (result is null)
            await cache.RemoveAsync(cacheKey, ct);

        return result;
    }

    public async Task<AggregatedSub?> GetSubAsync(
        int steamSubId, string region = "br", CancellationToken ct = default)
    {
        var cacheKey = $"sub:{steamSubId}:{region}";
        var result = await cache.GetOrCreateAsync(
            cacheKey,
            async ct =>
            {
                logger.LogDebug("Cache miss: {CacheKey}", cacheKey);
                return await inner.GetSubAsync(steamSubId, region, ct);
            },
            GameOptions,
            cancellationToken: ct);

        if (result is null)
            await cache.RemoveAsync(cacheKey, ct);

        return result;
    }

    public async Task<AggregatedGame?> SearchByTitleAsync(
        string title, string region = "br", CancellationToken ct = default)
    {
        var game = await inner.SearchByTitleAsync(title, region, ct);

        // Grava no cache sob a chave do App ID para que lookups diretos subsequentes
        // também se beneficiem do cache (sem chamar as APIs novamente)
        if (game is not null)
        {
            await cache.SetAsync(
                $"game:{game.SteamAppId}:{region}", game, GameOptions, cancellationToken: ct);
            tracker.RecordAccess(game.SteamAppId, region);
        }

        return game;
    }
}
