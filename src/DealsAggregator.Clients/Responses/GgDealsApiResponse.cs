using System.Text.Json.Serialization;

namespace DealsAggregator.Clients.Responses;

internal sealed class GgDealsApiResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; init; }

    [JsonPropertyName("data")]
    public Dictionary<string, GgDealsGameData?>? Data { get; init; }
}

internal sealed class GgDealsGameData
{
    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; init; } = string.Empty;

    [JsonPropertyName("prices")]
    public GgDealsGamePrices? Prices { get; init; }
}

internal sealed class GgDealsGamePrices
{
    [JsonPropertyName("currentRetail")]
    public string? CurrentRetail { get; init; }

    [JsonPropertyName("currentKeyshops")]
    public string? CurrentKeyshops { get; init; }

    [JsonPropertyName("historicalRetail")]
    public string? HistoricalRetail { get; init; }

    [JsonPropertyName("historicalKeyshops")]
    public string? HistoricalKeyshops { get; init; }

    [JsonPropertyName("currency")]
    public string Currency { get; init; } = string.Empty;
}
