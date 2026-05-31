using SteamDealX.Clients.Abstractions;
using SteamDealX.Clients.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace SteamDealX.Clients.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSteamDealXClients(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<GgDealsOptions>(configuration.GetSection(GgDealsOptions.Section));
        services.Configure<ItadOptions>(configuration.GetSection(ItadOptions.Section));

        services.AddHttpClient<IGgDealsClient, GgDealsClient>((sp, client) =>
        {
            client.BaseAddress = new Uri(sp.GetRequiredService<IOptions<GgDealsOptions>>().Value.BaseUrl);
        })
        .AddStandardResilienceHandler();

        services.AddHttpClient<IItadClient, ItadClient>((sp, client) =>
        {
            client.BaseAddress = new Uri(sp.GetRequiredService<IOptions<ItadOptions>>().Value.BaseUrl);
        })
        .AddStandardResilienceHandler();

        return services;
    }
}
