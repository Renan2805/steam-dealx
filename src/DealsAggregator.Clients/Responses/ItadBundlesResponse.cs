using System.Text.Json.Serialization;

namespace SteamDealX.Clients.Responses;

internal sealed class ItadBundleEntry
{
    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; init; } = string.Empty;

    [JsonPropertyName("page")]
    public ItadBundleEntryPage? Page { get; init; }
}

internal sealed class ItadBundleEntryPage
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;
}
