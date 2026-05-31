namespace SteamDealX.Clients.Options;

public sealed class ItadOptions
{
    public const string Section = "Itad";
    public string ApiKey { get; init; } = string.Empty;
    public string BaseUrl { get; init; } = "https://api.isthereanydeal.com/";
    public string DefaultCountry { get; init; } = "BR";
}
