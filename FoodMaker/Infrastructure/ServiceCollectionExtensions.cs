using System.Net;
using FoodMaker.Providers;
using FoodMaker.Providers.Strategies;
using FoodMaker.Services;
using Microsoft.Extensions.Caching.Memory;
using Polly;
using Polly.Extensions.Http;

namespace FoodMaker.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMealProviders(this IServiceCollection services, IConfiguration configuration)
    {
        var strategies = new IMealProviderStrategy[]
        {
            new BreakfastProviderStrategy(),
            new LunchProviderStrategy(),
            new DinnerProviderStrategy()
        };

        var providersConfig = configuration.GetSection("Providers");

        foreach (var strategy in strategies)
        {
            var baseAddress = providersConfig[strategy.ProviderName]!;
            services.AddHttpClient(strategy.ProviderName, httpClient =>
            {
                httpClient.BaseAddress = new Uri(baseAddress);
            })
            .AddPolicyHandler(GetRetryPolicy());

            services.AddSingleton<IMealProvider>(serviceProvider =>
                new CachedMealProvider(
                    new MealProvider(
                        serviceProvider.GetRequiredService<IHttpClientFactory>(),
                        strategy),
                    serviceProvider.GetRequiredService<IMemoryCache>()));
        }

        return services;
    }

    public static IServiceCollection AddMealServices(this IServiceCollection services)
    {
        services.AddScoped<MealService>();
        return services;
    }

    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(response => response.StatusCode == HttpStatusCode.RequestTimeout)
            .WaitAndRetryAsync(3, retryAttempt =>
                TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))
                + TimeSpan.FromMilliseconds(Random.Shared.Next(0, 1000)));
    }
}
