using FoodMaker.Domain;
using FoodMaker.Providers.Results;

namespace FoodMaker.Providers;

public interface IMealProvider
{
    MealType MealType { get; }

    Task<ProviderResult> GetIngredientsAsync(TimeOnly time, CancellationToken cancellationToken);
}
