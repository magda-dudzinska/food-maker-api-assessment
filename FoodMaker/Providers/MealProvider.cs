using System.Net;
using FoodMaker.Domain;
using FoodMaker.Providers.Results;
using FoodMaker.Providers.Strategies;

namespace FoodMaker.Providers;

public class MealProvider : IMealProvider
{
    private readonly IHttpClientFactory httpClientFactory;
    private readonly IMealProviderStrategy strategy;

    public MealProvider(IHttpClientFactory httpClientFactory, IMealProviderStrategy strategy)
    {
        this.httpClientFactory = httpClientFactory;
        this.strategy = strategy;
    }

    public MealType MealType => strategy.MealType;

    public async Task<ProviderResult> GetIngredientsAsync(TimeOnly time, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var sendResult = await SendRequest(time, cancellationToken);
        if (sendResult.Error is not null)
        {
            return sendResult.Error;
        }

        var response = sendResult.Response!;
        if (strategy.TreatBadRequestAsMealNotServed && response.StatusCode == HttpStatusCode.BadRequest)
        {
            return ProviderResult.Failure(new ProviderError(
                ProviderErrorKind.MealNotServed,
                $"{strategy.MealType} is not served",
                strategy.MealType,
                time));
        }

        if (!response.IsSuccessStatusCode)
        {
            return ProviderResult.Failure(new ProviderError(
                ProviderErrorKind.ProviderUnavailable,
                $"{strategy.ProviderName} returned {(int)response.StatusCode}",
                strategy.MealType));
        }

        var ingredients = await strategy.MapResponse(response.Content, cancellationToken);

        if (ingredients.Count == 0)
        {
            return ProviderResult.Failure(new ProviderError(
                ProviderErrorKind.MealNotServed,
                $"{strategy.MealType} is not served",
                strategy.MealType,
                time));
        }

        return ProviderResult.Success(ingredients);
    }

    private async Task<(HttpResponseMessage? Response, ProviderResult? Error)> SendRequest(
        TimeOnly time, CancellationToken cancellationToken)
    {
        try
        {
            var httpClient = httpClientFactory.CreateClient(strategy.ProviderName);
            var requestPath = strategy.BuildRequestPath(time);
            var response = await httpClient.GetAsync(requestPath, cancellationToken);
            return (response, null);
        }
        catch (HttpRequestException)
        {
            return (null, ProviderResult.Failure(new ProviderError(
                ProviderErrorKind.ProviderUnavailable,
                $"{strategy.ProviderName} is unavailable",
                strategy.MealType)));
        }
    }
}
