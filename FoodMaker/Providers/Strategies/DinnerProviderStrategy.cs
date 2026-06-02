using System.Net.Http.Json;
using FoodMaker.Domain;
using FoodMaker.Providers.Responses;

namespace FoodMaker.Providers.Strategies;

public class DinnerProviderStrategy : IMealProviderStrategy
{
    public MealType MealType => MealType.Dinner;
    public string ProviderName => "Dinner";
    public bool TreatBadRequestAsMealNotServed => true;

    public string BuildRequestPath(TimeOnly time) => $"components/{time.Hour}";

    public async Task<IReadOnlyList<string>> MapResponse(HttpContent content, CancellationToken cancellationToken)
    {
        var dinnerResponse = await content.ReadFromJsonAsync<DinnerResponse>(cancellationToken);
        return dinnerResponse?.DinnerSet?.Components?.Select(component => component.Name).ToList() ?? [];
    }
}
