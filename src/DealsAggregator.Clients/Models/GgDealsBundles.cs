namespace DealsAggregator.Clients.Models;

public sealed record GgDealsBundles(
    string Title,
    IReadOnlyList<GgDealsBundle> Active);

public sealed record GgDealsBundle(
    string BundleTitle,
    string Url,
    string Store);
