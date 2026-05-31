namespace SteamDealX.Clients;

/// <summary>
/// Lançada quando uma API upstream (gg.deals ou ITAD) retorna uma resposta de erro.
/// </summary>
public sealed class UpstreamApiException(string upstreamSource, int statusCode, string responseBody)
    : Exception($"{upstreamSource} returned HTTP {statusCode}: {responseBody}")
{
    public string UpstreamSource { get; } = upstreamSource;
    public int StatusCode { get; } = statusCode;
    public string ResponseBody { get; } = responseBody;
}
