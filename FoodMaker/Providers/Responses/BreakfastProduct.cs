using System.Text.Json.Serialization;

namespace FoodMaker.Providers.Responses;

internal sealed record BreakfastProduct(
    [property: JsonPropertyName("name")] string Name);
