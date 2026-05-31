using System.Text.Json.Serialization;

namespace SteamDealX.Clients.Responses;

internal sealed class ItadLookupResponse
{
    [JsonPropertyName("found")]
    public bool Found { get; init; }

    [JsonPropertyName("game")]
    public ItadGameInfo? Game { get; init; }
}

internal sealed class ItadGameInfo
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    [JsonPropertyName("slug")]
    public string Slug { get; init; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;
}
