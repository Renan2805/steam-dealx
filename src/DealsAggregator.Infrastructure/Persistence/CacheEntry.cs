namespace DealsAggregator.Infrastructure.Persistence;

internal sealed class CacheEntry
{
    public string Key { get; set; } = string.Empty;
    public byte[] Value { get; set; } = [];
    public DateTimeOffset? AbsoluteExpiry { get; set; }
    public long? SlidingExpirySeconds { get; set; }
    public DateTimeOffset LastAccessed { get; set; } = DateTimeOffset.UtcNow;
}
