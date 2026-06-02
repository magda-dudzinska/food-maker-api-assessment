using FoodMaker.Domain;

namespace FoodMaker.Providers.Results;

public sealed record ProviderError(
    ProviderErrorKind Kind,
    string Message,
    MealType MealType,
    TimeOnly? Time = null);
