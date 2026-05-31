using SteamDealX.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace SteamDealX.Infrastructure.Cache;

internal sealed class SqliteDistributedCache(IDbContextFactory<AppDbContext> factory) : IDistributedCache
{
    public byte[]? Get(string key)
    {
        using var db = factory.CreateDbContext();
        var entry = db.CacheEntries.Find(key);
        if (entry is null || IsExpired(entry)) return null;
        TouchSliding(db, entry);
        return entry.Value;
    }

    public async Task<byte[]?> GetAsync(string key, CancellationToken token = default)
    {
        await using var db = await factory.CreateDbContextAsync(token);
        var entry = await db.CacheEntries.FindAsync([key], token);
        if (entry is null || IsExpired(entry)) return null;
        await TouchSlidingAsync(db, entry, token);
        return entry.Value;
    }

    public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
    {
        using var db = factory.CreateDbContext();
        var entry = BuildEntry(key, value, options);
        db.CacheEntries.Update(entry);
        db.SaveChanges();
    }

    public async Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
    {
        await using var db = await factory.CreateDbContextAsync(token);
        var entry = BuildEntry(key, value, options);
        db.CacheEntries.Update(entry);
        await db.SaveChangesAsync(token);
    }

    public void Refresh(string key)
    {
        using var db = factory.CreateDbContext();
        var entry = db.CacheEntries.Find(key);
        if (entry is not null && !IsExpired(entry))
            TouchSliding(db, entry);
    }

    public async Task RefreshAsync(string key, CancellationToken token = default)
    {
        await using var db = await factory.CreateDbContextAsync(token);
        var entry = await db.CacheEntries.FindAsync([key], token);
        if (entry is not null && !IsExpired(entry))
            await TouchSlidingAsync(db, entry, token);
    }

    public void Remove(string key)
    {
        using var db = factory.CreateDbContext();
        db.CacheEntries.Where(e => e.Key == key).ExecuteDelete();
    }

    public async Task RemoveAsync(string key, CancellationToken token = default)
    {
        await using var db = await factory.CreateDbContextAsync(token);
        await db.CacheEntries.Where(e => e.Key == key).ExecuteDeleteAsync(token);
    }

    // ---------------------------------------------------------------------------

    private static bool IsExpired(CacheEntry entry)
    {
        var now = DateTimeOffset.UtcNow;

        if (entry.AbsoluteExpiry.HasValue && now >= entry.AbsoluteExpiry.Value)
            return true;

        if (entry.SlidingExpirySeconds.HasValue)
        {
            var slidingDeadline = entry.LastAccessed.AddSeconds(entry.SlidingExpirySeconds.Value);
            if (now >= slidingDeadline) return true;
        }

        return false;
    }

    private static CacheEntry BuildEntry(string key, byte[] value, DistributedCacheEntryOptions opts)
    {
        var now = DateTimeOffset.UtcNow;

        DateTimeOffset? absoluteExpiry = opts.AbsoluteExpiration
            ?? (opts.AbsoluteExpirationRelativeToNow.HasValue
                ? now + opts.AbsoluteExpirationRelativeToNow.Value
                : null);

        return new CacheEntry
        {
            Key                 = key,
            Value               = value,
            AbsoluteExpiry      = absoluteExpiry,
            SlidingExpirySeconds = (long?)opts.SlidingExpiration?.TotalSeconds,
            LastAccessed        = now,
        };
    }

    private static void TouchSliding(AppDbContext db, CacheEntry entry)
    {
        if (entry.SlidingExpirySeconds is null) return;
        entry.LastAccessed = DateTimeOffset.UtcNow;
        db.SaveChanges();
    }

    private static async Task TouchSlidingAsync(AppDbContext db, CacheEntry entry, CancellationToken token)
    {
        if (entry.SlidingExpirySeconds is null) return;
        entry.LastAccessed = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(token);
    }
}
