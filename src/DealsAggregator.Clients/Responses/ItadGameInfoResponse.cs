using System.Text.Json.Serialization;

namespace DealsAggregator.Clients.Responses;

internal sealed class ItadGameInfoResponse
{
    // O campo appid é null para jogos não disponíveis na Steam (ex: exclusivos GOG)
    [JsonPropertyName("appid")]
    public int? Appid { get; init; }
}
