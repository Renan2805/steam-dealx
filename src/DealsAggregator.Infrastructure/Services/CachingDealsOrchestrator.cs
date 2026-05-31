using DealsAggregator.Core.Abstractions;
using DealsAggregator.Core.Models;
using Microsoft.Extensions.Caching.Hybrid;

namespace DealsAggregator.Infrastructure.Services;

internal sealed class CachingDealsOrchestrator(
    DealsOrchestrator inner,
    HybridCache cache,
    IPopularGamesTracker tracker) : IDealsOrchestrator
{
    private static readonly HybridCacheEntryOptions GameOptions = new()
    {
        Expiration           = TimeSpan.FromHours(2),
        LocalCacheExpiration = TimeSpan.FromMinutes(10),
    };

    public async Task<AggregatedGame?> GetGameAsync(
        int steamAppId, string region = "br", CancellationToken ct = default)
    {
        var result = await cache.GetOrCreateAsync(
            $"game:{steamAppId}:{region}",
            async ct => await inner.GetGameAsync(steamAppId, region, ct),
            GameOptions,
            cancellationToken: ct);

        if (result is null)
            await cache.RemoveAsync($"game:{steamAppId}:{region}", ct);
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
            return cached;

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

        return cached;
    }

    public Task<AggregatedGame?> SearchByTitleAsync(
        string title, string region = "br", CancellationToken ct = default)
        => inner.SearchByTitleAsync(title, region, ct);
}
