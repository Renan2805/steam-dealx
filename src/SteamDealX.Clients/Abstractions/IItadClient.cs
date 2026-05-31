using SteamDealX.Clients.Models;

namespace SteamDealX.Clients.Abstractions;

public interface IItadClient
{
    Task<Guid?> LookupBySteamAppIdAsync(int steamAppId, CancellationToken ct = default);
    Task<Guid?> LookupByTitleAsync(string title, CancellationToken ct = default);

    // Resolve UUID ITAD → Steam App ID via /games/info/v2. Retorna null para jogos sem Steam.
    Task<int?> GetSteamAppIdAsync(Guid itadUuid, CancellationToken ct = default);

    // Resolve sub/{steamSubId} → UUID ITAD via POST /lookup/id/shop/61/v1
    Task<Guid?> LookupBySteamSubIdAsync(int steamSubId, CancellationToken ct = default);

    // Resolve bundle/{steamBundleId} → UUID ITAD via POST /lookup/id/shop/61/v1
    Task<Guid?> LookupBySteamBundleIdAsync(int steamBundleId, CancellationToken ct = default);

    // GET /games/bundles/v2 — bundles ativos que contêm o jogo (vazio se nenhum)
    Task<IReadOnlyList<ItadBundle>> GetGameBundlesAsync(
        Guid itadUuid, string country = "BR", CancellationToken ct = default);

    // Retorna deals + historyLow.all por UUID — historyLow já vem embutido na resposta de /prices/v3
    Task<IReadOnlyDictionary<Guid, ItadGamePrices>> GetPricesAsync(
        IReadOnlyCollection<Guid> gameIds,
        string country = "BR",
        CancellationToken ct = default);
}
