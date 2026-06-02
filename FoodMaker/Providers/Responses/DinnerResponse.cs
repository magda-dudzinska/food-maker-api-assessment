using System.Text.Json.Serialization;

namespace FoodMaker.Providers.Responses;

internal sealed record DinnerResponse(
    [property: JsonPropertyName("dinnerSet")] DinnerSet? DinnerSet);
