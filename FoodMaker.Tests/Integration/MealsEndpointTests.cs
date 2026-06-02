using System.Net;
using System.Net.Http.Json;
using FoodMaker.Domain;
using FoodMaker.Providers;
using FoodMaker.Providers.Results;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Xunit;

namespace FoodMaker.Tests.Integration;

public class MealsEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly IMealProvider breakfastProvider = Substitute.For<IMealProvider>();
    private readonly IMealProvider lunchProvider = Substitute.For<IMealProvider>();
    private readonly IMealProvider dinnerProvider = Substitute.For<IMealProvider>();
    private readonly HttpClient httpClient;

    public MealsEndpointTests(WebApplicationFactory<Program> factory)
    {
        breakfastProvider.MealType.Returns(MealType.Breakfast);
        lunchProvider.MealType.Returns(MealType.Lunch);
        dinnerProvider.MealType.Returns(MealType.Dinner);

        httpClient = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var existingProviders = services
                    .Where(descriptor => descriptor.ServiceType == typeof(IMealProvider))
                    .ToList();
                foreach (var descriptor in existingProviders)
                {
                    services.Remove(descriptor);
                }

                services.AddSingleton(breakfastProvider);
                services.AddSingleton(lunchProvider);
                services.AddSingleton(dinnerProvider);
            });
        }).CreateClient();
    }

    [Fact]
    public async Task GetMeals_BreakfastWith200_ReturnsIngredientsArray()
    {
        breakfastProvider
            .GetIngredientsAsync(Arg.Any<TimeOnly>(), Arg.Any<CancellationToken>())
            .Returns(ProviderResult.Success(new List<string> { "bread", "eggs" }));

        var response = await httpClient.GetAsync("/meals/Breakfast?time=08:00");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var ingredients = await response.Content.ReadFromJsonAsync<List<string>>();
        ingredients.ShouldBe(new[] { "bread", "eggs" });
    }

    [Fact]
    public async Task GetMeals_LowercaseMealType_MatchesCaseInsensitively()
    {
        lunchProvider
            .GetIngredientsAsync(Arg.Any<TimeOnly>(), Arg.Any<CancellationToken>())
            .Returns(ProviderResult.Success(new List<string> { "soup" }));

        var response = await httpClient.GetAsync("/meals/lunch?time=13:00");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var ingredients = await response.Content.ReadFromJsonAsync<List<string>>();
        ingredients.ShouldBe(new[] { "soup" });
    }

    [Fact]
    public async Task GetMeals_NoTimeParameter_Returns200WithCurrentTimeDefault()
    {
        dinnerProvider
            .GetIngredientsAsync(Arg.Any<TimeOnly>(), Arg.Any<CancellationToken>())
            .Returns(ProviderResult.Success(new List<string> { "steak" }));

        var response = await httpClient.GetAsync("/meals/Dinner");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var ingredients = await response.Content.ReadFromJsonAsync<List<string>>();
        ingredients.ShouldBe(new[] { "steak" });
    }

    [Fact]
    public async Task GetMeals_UnsupportedMealType_Returns400WithMessage()
    {
        var response = await httpClient.GetAsync("/meals/Brunch?time=08:00");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.ShouldContain("Brunch");
    }

    [Fact]
    public async Task GetMeals_InvalidTimeFormat_Returns400()
    {
        var response = await httpClient.GetAsync("/meals/Breakfast?time=invalid");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetMeals_ProviderReturnsMealNotServed_Returns404WithMessage()
    {
        breakfastProvider
            .GetIngredientsAsync(Arg.Any<TimeOnly>(), Arg.Any<CancellationToken>())
            .Returns(ProviderResult.Failure(new ProviderError(
                ProviderErrorKind.MealNotServed,
                "Breakfast is not served at 23:00",
                MealType.Breakfast,
                new TimeOnly(23, 0))));

        var response = await httpClient.GetAsync("/meals/Breakfast?time=23:00");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var body = await response.Content.ReadAsStringAsync();
        body.ShouldContain("Breakfast");
    }

    [Fact]
    public async Task GetMeals_ProviderReturnsProviderUnavailable_Returns502()
    {
        lunchProvider
            .GetIngredientsAsync(Arg.Any<TimeOnly>(), Arg.Any<CancellationToken>())
            .Returns(ProviderResult.Failure(new ProviderError(
                ProviderErrorKind.ProviderUnavailable,
                "Lunch is unavailable",
                MealType.Lunch)));

        var response = await httpClient.GetAsync("/meals/Lunch?time=12:00");

        response.StatusCode.ShouldBe(HttpStatusCode.BadGateway);
    }

    [Fact]
    public async Task GetMeals_ProviderReturnsUnsupportedMealType_Returns400()
    {
        breakfastProvider
            .GetIngredientsAsync(Arg.Any<TimeOnly>(), Arg.Any<CancellationToken>())
            .Returns(ProviderResult.Failure(new ProviderError(
                ProviderErrorKind.UnsupportedMealType,
                "No provider registered for meal type 'Breakfast'",
                MealType.Breakfast)));

        var response = await httpClient.GetAsync("/meals/Breakfast?time=08:00");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
