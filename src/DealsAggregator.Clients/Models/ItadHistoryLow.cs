namespace DealsAggregator.Clients.Models;

public sealed record ItadHistoryLow(
    decimal Amount,
    string Currency,
    DateTimeOffset? Timestamp);
