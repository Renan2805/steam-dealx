using DealsAggregator.Core.Abstractions;
using DealsAggregator.Infrastructure.Cache;
using DealsAggregator.Infrastructure.Persistence;
using DealsAggregator.Infrastructure.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DealsAggregator.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDealsAggregatorInfrastructure(
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

        // Decorator: CachingDealsOrchestrator envolve DealsOrchestrator
        services.AddScoped<DealsOrchestrator>();
        services.AddScoped<IDealsOrchestrator>(sp =>
            new CachingDealsOrchestrator(
                sp.GetRequiredService<DealsOrchestrator>(),
                sp.GetRequiredService<Microsoft.Extensions.Caching.Hybrid.HybridCache>()));

        return services;
    }

    // Inicializa o schema do SQLite — chamar em Program.cs após o build
    public static IApplicationBuilder UseDealsAggregatorInfrastructure(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();
        return app;
    }
}
