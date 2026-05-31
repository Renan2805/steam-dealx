using SteamDealX.Core.Models;

namespace SteamDealX.Core.Abstractions;

public interface IDealsOrchestrator
{
    Task<AggregatedGame?> GetGameAsync(
        int steamAppId, string region = "br", CancellationToken ct = default);

    Task<IReadOnlyDictionary<int, AggregatedGame?>> GetGamesBatchAsync(
        IReadOnlyCollection<int> steamAppIds, string region = "br", CancellationToken ct = default);

    Task<AggregatedGame?> SearchByTitleAsync(
        string title, string region = "br", CancellationToken ct = default);

    Task<AggregatedBundle?> GetBundleAsync(
        int steamBundleId, string region = "br", CancellationToken ct = default);

    Task<AggregatedSub?> GetSubAsync(
        int steamSubId, string region = "br", CancellationToken ct = default);
}
