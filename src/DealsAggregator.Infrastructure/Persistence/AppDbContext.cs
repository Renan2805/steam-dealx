using Microsoft.EntityFrameworkCore;

namespace DealsAggregator.Infrastructure.Persistence;

internal sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<CacheEntry> CacheEntries => Set<CacheEntry>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<CacheEntry>(e =>
        {
            e.HasKey(x => x.Key);
            e.Property(x => x.Value).IsRequired();
        });
    }
}
