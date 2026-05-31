using System.Text.Json.Serialization;

namespace DealsAggregator.Clients.Responses;

internal sealed class ItadHistoryLowResponse
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    [JsonPropertyName("low")]
    public ItadLow? Low { get; init; }
}

internal sealed class ItadLow
{
    [JsonPropertyName("price")]
    public required ItadMoney Price { get; init; }

    [JsonPropertyName("timestamp")]
    public DateTimeOffset? Timestamp { get; init; }
}
