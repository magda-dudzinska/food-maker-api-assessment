using System.Text.Json.Serialization;

namespace FoodMaker.Providers.Responses;

internal sealed record DinnerComponent(
    [property: JsonPropertyName("name")] string Name);
