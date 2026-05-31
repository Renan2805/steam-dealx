using SteamDealX.Core.Abstractions;
using SteamDealX.Infrastructure.Cache;
using SteamDealX.Infrastructure.Persistence;
using SteamDealX.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace SteamDealX.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSteamDealXInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var dbPath = configuration["Cache:DbPath"] ?? "dealscache.db";

        services.AddDbContextFactory<AppDbContext>(opts =>
            opts.UseSqlite($"Data Source={dbPath}"));

        services.AddSingleton<IDistributedCache, SqliteDistributedCache>();

        services.AddHybridCache(opts =>
        {
            opts.DefaultEntryOptions = new()
            {
                Expiration           = TimeSpan.FromHours(2),
                LocalCacheExpiration = TimeSpan.FromMinutes(10),
            };
        });

        // Tracker de popularidade: singleton para sobreviver entre requests
        services.AddSingleton<IPopularGamesTracker, PopularGamesTracker>();

        // BackgroundService de reaquecimento de cache
        services.AddHostedService<CacheWarmupService>();

        // Decorator: CachingDealsOrchestrator envolve DealsOrchestrator
        services.AddScoped<DealsOrchestrator>();
        services.AddScoped<IDealsOrchestrator>(sp =>
            new CachingDealsOrchestrator(
                sp.GetRequiredService<DealsOrchestrator>(),
                sp.GetRequiredService<HybridCache>(),
                sp.GetRequiredService<IPopularGamesTracker>(),
                sp.GetRequiredService<ILogger<CachingDealsOrchestrator>>()));

        return services;
    }

    public static void EnsureDatabase(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();
    }
}
