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

public class LunchProviderTests
{
    private readonly Mock<HttpMessageHandler> handlerMock = new();
    private readonly MealProvider lunchProvider;

    public LunchProviderTests()
    {
        var strategy = new LunchProviderStrategy();
        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("https://lunch.local/")
        };
        var httpClientFactory = new Mock<IHttpClientFactory>();
        httpClientFactory
            .Setup(factory => factory.CreateClient(strategy.ProviderName))
            .Returns(httpClient);
        lunchProvider = new MealProvider(httpClientFactory.Object, strategy);
    }

    [Fact]
    public async Task GetIngredientsAsync_On200WithItems_ReturnsSuccessWithIngredientNames()
    {
        var json = """{"items": ["soup", "potatoes", "salad"]}""";
        SetupResponse(HttpStatusCode.OK, json);

        var result = await lunchProvider.GetIngredientsAsync(new TimeOnly(12, 0), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Ingredients.ShouldBe(new[] { "soup", "potatoes", "salad" });
    }

    [Fact]
    public async Task GetIngredientsAsync_SendsHourQueryParameterAsInteger()
    {
        var json = """{"items": []}""";
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

        await lunchProvider.GetIngredientsAsync(new TimeOnly(14, 30), CancellationToken.None);

        captured.ShouldNotBeNull();
        captured.RequestUri!.Query.ShouldContain("hour=14");
    }

    [Fact]
    public async Task GetIngredientsAsync_On400_ReturnsMealNotServedError()
    {
        SetupResponse(HttpStatusCode.BadRequest, "");

        var result = await lunchProvider.GetIngredientsAsync(new TimeOnly(23, 0), CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error.Kind.ShouldBe(ProviderErrorKind.MealNotServed);
        result.Error.MealType.ShouldBe(MealType.Lunch);
        result.Error.Time.ShouldBe(new TimeOnly(23, 0));
    }

    [Fact]
    public async Task GetIngredientsAsync_On500_ReturnsProviderUnavailableError()
    {
        SetupResponse(HttpStatusCode.InternalServerError, "");

        var result = await lunchProvider.GetIngredientsAsync(new TimeOnly(12, 0), CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error.Kind.ShouldBe(ProviderErrorKind.ProviderUnavailable);
        result.Error.MealType.ShouldBe(MealType.Lunch);
    }

    [Fact]
    public async Task GetIngredientsAsync_WhenTokenIsCancelled_ThrowsOperationCanceledException()
    {
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        await Should.ThrowAsync<OperationCanceledException>(
            () => lunchProvider.GetIngredientsAsync(new TimeOnly(12, 0), cancellationTokenSource.Token));
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

        var result = await lunchProvider.GetIngredientsAsync(new TimeOnly(12, 0), CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error.Kind.ShouldBe(ProviderErrorKind.ProviderUnavailable);
        result.Error.MealType.ShouldBe(MealType.Lunch);
    }

    [Fact]
    public void MealType_ReturnsLunch()
    {
        lunchProvider.MealType.ShouldBe(MealType.Lunch);
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
