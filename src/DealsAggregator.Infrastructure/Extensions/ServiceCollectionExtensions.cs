using DealsAggregator.Core.Abstractions;
using DealsAggregator.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DealsAggregator.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDealsAggregatorInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHybridCache();
        services.AddScoped<IDealsOrchestrator, DealsOrchestrator>();
        return services;
    }
}
