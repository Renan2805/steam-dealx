namespace DealsAggregator.Clients.Options;

public sealed class GgDealsOptions
{
    public const string Section = "GgDeals";
    public string ApiKey { get; init; } = string.Empty;
    public string BaseUrl { get; init; } = "https://gg.deals/api/";
    public string DefaultRegion { get; init; } = "br";
}
