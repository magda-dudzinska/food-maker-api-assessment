using System.Text.Json.Serialization;

namespace FoodMaker.Providers.Responses;

internal sealed record LunchResponse(
    [property: JsonPropertyName("items")] IReadOnlyList<string> Items);
