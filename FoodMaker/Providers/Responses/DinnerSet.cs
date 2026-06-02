using System.Text.Json.Serialization;

namespace FoodMaker.Providers.Responses;

internal sealed record DinnerSet(
    [property: JsonPropertyName("components")] IReadOnlyList<DinnerComponent>? Components);
