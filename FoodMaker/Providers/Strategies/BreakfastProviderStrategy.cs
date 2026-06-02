using System.Net.Http.Json;
using FoodMaker.Domain;
using FoodMaker.Providers.Responses;

namespace FoodMaker.Providers.Strategies;

public class BreakfastProviderStrategy : IMealProviderStrategy
{
    public MealType MealType => MealType.Breakfast;
    public string ProviderName => "Breakfast";
    public bool TreatBadRequestAsMealNotServed => false;

    public string BuildRequestPath(TimeOnly time) => $"ingridients?time={time:HH:mm}";

    public async Task<IReadOnlyList<string>> MapResponse(HttpContent content, CancellationToken cancellationToken)
    {
        var breakfastResponse = await content.ReadFromJsonAsync<BreakfastResponse>(cancellationToken);
        return breakfastResponse?.Products.Select(product => product.Name).ToList() ?? [];
    }
}
