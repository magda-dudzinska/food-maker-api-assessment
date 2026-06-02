using FoodMaker.Domain;
using FoodMaker.Providers.Results;
using Microsoft.Extensions.Caching.Memory;

namespace FoodMaker.Providers;

public sealed class CachedMealProvider : IMealProvider
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    private readonly IMealProvider innerProvider;
    private readonly IMemoryCache cache;

    public CachedMealProvider(IMealProvider innerProvider, IMemoryCache cache)
    {
        this.innerProvider = innerProvider;
        this.cache = cache;
    }

    public MealType MealType => innerProvider.MealType;

    public async Task<ProviderResult> GetIngredientsAsync(TimeOnly time, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var cacheKey = (innerProvider.MealType, time);

        if (cache.TryGetValue(cacheKey, out ProviderResult? cachedResult))
        {
            return cachedResult!;
        }

        var result = await innerProvider.GetIngredientsAsync(time, cancellationToken);

        if (result.IsSuccess)
        {
            cache.Set(cacheKey, result, CacheDuration);
        }

        return result;
    }
}
