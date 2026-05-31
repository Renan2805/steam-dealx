using System.Text.Json.Serialization;

namespace DealsAggregator.Clients.Responses;

internal sealed class ItadPricesResponse
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    [JsonPropertyName("deals")]
    public IReadOnlyList<ItadDeal> Deals { get; init; } = [];
}

internal sealed class ItadDeal
{
    [JsonPropertyName("shop")]
    public required ItadShop Shop { get; init; }

    [JsonPropertyName("price")]
    public required ItadMoney Price { get; init; }

    [JsonPropertyName("regular")]
    public ItadMoney? Regular { get; init; }

    [JsonPropertyName("cut")]
    public int Cut { get; init; }

    [JsonPropertyName("url")]
    public string Url { get; init; } = string.Empty;
}

internal sealed class ItadShop
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;
}

internal sealed class ItadMoney
{
    [JsonPropertyName("amount")]
    public decimal Amount { get; init; }

    [JsonPropertyName("currency")]
    public string Currency { get; init; } = string.Empty;
}
