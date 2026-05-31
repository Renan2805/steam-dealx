using DealsAggregator.Clients.Abstractions;
using DealsAggregator.Clients.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DealsAggregator.Clients.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDealsAggregatorClients(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<GgDealsOptions>(configuration.GetSection(GgDealsOptions.Section));
        services.Configure<ItadOptions>(configuration.GetSection(ItadOptions.Section));

        services.AddHttpClient<IGgDealsClient, GgDealsClient>()
            .AddStandardResilienceHandler();

        services.AddHttpClient<IItadClient, ItadClient>()
            .AddStandardResilienceHandler();

        return services;
    }
}
