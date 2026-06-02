using FoodMaker.Domain;
using FoodMaker.Providers;
using FoodMaker.Providers.Results;

namespace FoodMaker.Services;

public sealed class MealService
{
    private readonly Dictionary<MealType, IMealProvider> providersByMealType;

    public MealService(IEnumerable<IMealProvider> providers)
    {
        providersByMealType = providers.ToDictionary(provider => provider.MealType);
    }

    public Task<ProviderResult> GetIngredientsAsync(MealType mealType, TimeOnly time, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!providersByMealType.TryGetValue(mealType, out var provider))
        {
            return Task.FromResult(ProviderResult.Failure(new ProviderError(
                ProviderErrorKind.UnsupportedMealType,
                $"No provider registered for meal type '{mealType}'",
                mealType)));
        }

        return provider.GetIngredientsAsync(time, cancellationToken);
    }
}
