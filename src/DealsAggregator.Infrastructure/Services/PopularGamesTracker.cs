using System.Collections.Concurrent;

namespace DealsAggregator.Infrastructure.Services;

/// <summary>
/// Singleton thread-safe que rastreia quantas vezes cada jogo foi acessado.
/// Usado pelo BackgroundService de reaquecimento para priorizar os jogos mais populares.
/// </summary>
internal sealed class PopularGamesTracker : IPopularGamesTracker
{
    private readonly ConcurrentDictionary<(int SteamAppId, string Region), long> _counts = new();

    public void RecordAccess(int steamAppId, string region) =>
        _counts.AddOrUpdate((steamAppId, region), 1L, (_, count) => count + 1L);

    public IReadOnlyList<(int SteamAppId, string Region)> GetTopGames(int count) =>
        [.. _counts
            .OrderByDescending(kvp => kvp.Value)
            .Take(count)
            .Select(kvp => kvp.Key)];
}
