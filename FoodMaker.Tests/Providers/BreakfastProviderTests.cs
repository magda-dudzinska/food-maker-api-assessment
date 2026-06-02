using System.Net;
using FoodMaker.Domain;
using FoodMaker.Providers;
using FoodMaker.Providers.Results;
using FoodMaker.Providers.Strategies;
using Moq;
using Moq.Protected;
using Shouldly;
using Xunit;

namespace FoodMaker.Tests.Providers;

public class BreakfastProviderTests
{
    private readonly Mock<HttpMessageHandler> handlerMock = new();
    private readonly MealProvider breakfastProvider;

    public BreakfastProviderTests()
    {
        var strategy = new BreakfastProviderStrategy();
        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("https://breakfast.local/")
        };
        var httpClientFactory = new Mock<IHttpClientFactory>();
        httpClientFactory
            .Setup(factory => factory.CreateClient(strategy.ProviderName))
            .Returns(httpClient);
        breakfastProvider = new MealProvider(httpClientFactory.Object, strategy);
    }

    [Fact]
    public async Task GetIngredientsAsync_On200WithProducts_ReturnsSuccessWithIngredientNames()
    {
        var json = """{"products": [{"name":"bread"}, {"name":"eggs"}]}""";
        SetupResponse(HttpStatusCode.OK, json);

        var result = await breakfastProvider.GetIngredientsAsync(new TimeOnly(8, 0), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Ingredients.ShouldBe(new[] { "bread", "eggs" });
    }

    [Fact]
    public async Task GetIngredientsAsync_On200WithEmptyProducts_ReturnsMealNotServedError()
    {
        var json = """{"products": []}""";
        SetupResponse(HttpStatusCode.OK, json);

        var result = await breakfastProvider.GetIngredientsAsync(new TimeOnly(23, 0), CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error.Kind.ShouldBe(ProviderErrorKind.MealNotServed);
        result.Error.MealType.ShouldBe(MealType.Breakfast);
        result.Error.Time.ShouldBe(new TimeOnly(23, 0));
    }

    [Fact]
    public async Task GetIngredientsAsync_SendsTimeQueryParameterFormattedAsHHmm()
    {
        var json = """{"products": []}""";
        HttpRequestMessage? captured = null;

        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((request, _) => captured = request)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            });

        await breakfastProvider.GetIngredientsAsync(new TimeOnly(8, 0), CancellationToken.None);

        captured.ShouldNotBeNull();
        captured.RequestUri!.Query.ShouldContain("time=08:00");
    }

    [Fact]
    public async Task GetIngredientsAsync_WhenTokenIsCancelled_ThrowsOperationCanceledException()
    {
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        await Should.ThrowAsync<OperationCanceledException>(
            () => breakfastProvider.GetIngredientsAsync(new TimeOnly(8, 0), cancellationTokenSource.Token));
    }

    [Fact]
    public async Task GetIngredientsAsync_On500_ReturnsProviderUnavailableError()
    {
        SetupResponse(HttpStatusCode.InternalServerError, "");

        var result = await breakfastProvider.GetIngredientsAsync(new TimeOnly(8, 0), CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error.Kind.ShouldBe(ProviderErrorKind.ProviderUnavailable);
        result.Error.MealType.ShouldBe(MealType.Breakfast);
    }

    [Fact]
    public async Task GetIngredientsAsync_OnNetworkFailure_ReturnsProviderUnavailableError()
    {
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        var result = await breakfastProvider.GetIngredientsAsync(new TimeOnly(8, 0), CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error.Kind.ShouldBe(ProviderErrorKind.ProviderUnavailable);
        result.Error.MealType.ShouldBe(MealType.Breakfast);
    }

    [Fact]
    public void MealType_ReturnsBreakfast()
    {
        breakfastProvider.MealType.ShouldBe(MealType.Breakfast);
    }

    private void SetupResponse(HttpStatusCode statusCode, string json)
    {
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            });
    }
}
