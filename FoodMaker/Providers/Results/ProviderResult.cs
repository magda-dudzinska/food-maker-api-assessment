namespace FoodMaker.Providers.Results;

public sealed record ProviderResult
{
    public IReadOnlyList<string> Ingredients { get; }
    public ProviderError? Error { get; }
    public bool IsSuccess => Error is null;

    private ProviderResult(IReadOnlyList<string> ingredients, ProviderError? error)
    {
        Ingredients = ingredients;
        Error = error;
    }

    public static ProviderResult Success(IReadOnlyList<string> ingredients) =>
        new(ingredients, null);

    public static ProviderResult Failure(ProviderError error) =>
        new([], error);
}
