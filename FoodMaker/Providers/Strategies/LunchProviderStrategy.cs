using System.Net.Http.Json;
using FoodMaker.Domain;
using FoodMaker.Providers.Responses;

namespace FoodMaker.Providers.Strategies;

public class LunchProviderStrategy : IMealProviderStrategy
{
    public MealType MealType => MealType.Lunch;
    public string ProviderName => "Lunch";
    public bool TreatBadRequestAsMealNotServed => true;

    public string BuildRequestPath(TimeOnly time) => $"items?hour={time.Hour}";

    public async Task<IReadOnlyList<string>> MapResponse(HttpContent content, CancellationToken cancellationToken)
    {
        var lunchResponse = await content.ReadFromJsonAsync<LunchResponse>(cancellationToken);
        return lunchResponse?.Items.ToList() ?? [];
    }
}
