namespace SteamDealX.Infrastructure.Services;

internal interface IPopularGamesTracker
{
    void RecordAccess(int steamAppId, string region);
    IReadOnlyList<(int SteamAppId, string Region)> GetTopGames(int count);
}
