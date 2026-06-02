using FoodMaker.Domain;
using FoodMaker.Providers;
using FoodMaker.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace FoodMaker.Tests.Integration;

public class ServiceRegistrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly IServiceProvider serviceProvider;

    public ServiceRegistrationTests(WebApplicationFactory<Program> factory)
    {
        serviceProvider = factory.Services.CreateScope().ServiceProvider;
    }

    [Fact]
    public void ResolvingMealProviders_ReturnsExactlyThreeProviders()
    {
        var providers = serviceProvider.GetServices<IMealProvider>();

        providers.Count().ShouldBe(3);
    }

    [Fact]
    public void ResolvingMealProviders_CoversAllMealTypes()
    {
        var providers = serviceProvider.GetServices<IMealProvider>();
        var mealTypes = providers.Select(provider => provider.MealType).ToList();

        mealTypes.ShouldContain(MealType.Breakfast);
        mealTypes.ShouldContain(MealType.Lunch);
        mealTypes.ShouldContain(MealType.Dinner);
    }

    [Fact]
    public void ResolvingMealService_DoesNotThrow()
    {
        var mealService = serviceProvider.GetService<MealService>();

        mealService.ShouldNotBeNull();
    }

    [Theory]
    [InlineData("Breakfast", "http://localhost:5234/")]
    [InlineData("Lunch", "http://localhost:5032/")]
    [InlineData("Dinner", "http://localhost:5211/")]
    public void NamedHttpClient_HasCorrectBaseAddress(string clientName, string expectedBaseAddress)
    {
        var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        var httpClient = httpClientFactory.CreateClient(clientName);

        httpClient.BaseAddress.ShouldNotBeNull();
        httpClient.BaseAddress.ToString().ShouldBe(expectedBaseAddress);
    }
}
