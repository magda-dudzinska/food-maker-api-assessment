using System.Text.Json.Serialization;

namespace FoodMaker.Providers.Responses;

internal sealed record BreakfastResponse(
    [property: JsonPropertyName("products")] IReadOnlyList<BreakfastProduct> Products);
