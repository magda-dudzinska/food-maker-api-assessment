using FoodMaker.Domain;

namespace FoodMaker.Providers.Strategies;

public interface IMealProviderStrategy
{
    MealType MealType { get; }
    string ProviderName { get; }
    bool TreatBadRequestAsMealNotServed { get; }
    string BuildRequestPath(TimeOnly time);
    Task<IReadOnlyList<string>> MapResponse(HttpContent content, CancellationToken cancellationToken);
}
