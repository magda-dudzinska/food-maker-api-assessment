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

public class DinnerProviderTests
{
    private readonly Mock<HttpMessageHandler> handlerMock = new();
    private readonly MealProvider dinnerProvider;

    public DinnerProviderTests()
    {
        var strategy = new DinnerProviderStrategy();
        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("https://dinner.local/")
        };
        var httpClientFactory = new Mock<IHttpClientFactory>();
        httpClientFactory
            .Setup(factory => factory.CreateClient(strategy.ProviderName))
            .Returns(httpClient);
        dinnerProvider = new MealProvider(httpClientFactory.Object, strategy);
    }

    [Fact]
    public async Task GetIngredientsAsync_On200WithComponents_ReturnsSuccessWithIngredientNames()
    {
        var json = """{"dinnerSet": {"components": [{"name":"steak"}, {"name":"rice"}]}}""";
        SetupResponse(HttpStatusCode.OK, json);

        var result = await dinnerProvider.GetIngredientsAsync(new TimeOnly(21, 0), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Ingredients.ShouldBe(new[] { "steak", "rice" });
    }

    [Fact]
    public async Task GetIngredientsAsync_SendsHourAsUrlPathSegment()
    {
        var json = """{"dinnerSet": {"components": []}}""";
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

        await dinnerProvider.GetIngredientsAsync(new TimeOnly(21, 0), CancellationToken.None);

        captured.ShouldNotBeNull();
        captured.RequestUri!.AbsolutePath.ShouldEndWith("/components/21");
    }

    [Fact]
    public async Task GetIngredientsAsync_On400_ReturnsMealNotServedError()
    {
        SetupResponse(HttpStatusCode.BadRequest, "");

        var result = await dinnerProvider.GetIngredientsAsync(new TimeOnly(6, 0), CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error.Kind.ShouldBe(ProviderErrorKind.MealNotServed);
        result.Error.MealType.ShouldBe(MealType.Dinner);
        result.Error.Time.ShouldBe(new TimeOnly(6, 0));
    }

    [Fact]
    public async Task GetIngredientsAsync_On500_ReturnsProviderUnavailableError()
    {
        SetupResponse(HttpStatusCode.InternalServerError, "");

        var result = await dinnerProvider.GetIngredientsAsync(new TimeOnly(21, 0), CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error.Kind.ShouldBe(ProviderErrorKind.ProviderUnavailable);
        result.Error.MealType.ShouldBe(MealType.Dinner);
    }

    [Fact]
    public async Task GetIngredientsAsync_WhenTokenIsCancelled_ThrowsOperationCanceledException()
    {
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        await Should.ThrowAsync<OperationCanceledException>(
            () => dinnerProvider.GetIngredientsAsync(new TimeOnly(21, 0), cancellationTokenSource.Token));
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

        var result = await dinnerProvider.GetIngredientsAsync(new TimeOnly(21, 0), CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error.Kind.ShouldBe(ProviderErrorKind.ProviderUnavailable);
        result.Error.MealType.ShouldBe(MealType.Dinner);
    }

    [Fact]
    public void MealType_ReturnsDinner()
    {
        dinnerProvider.MealType.ShouldBe(MealType.Dinner);
    }

    [Theory]
    [InlineData("""{"dinnerSet": null}""")]
    [InlineData("""{"dinnerSet": {"components": null}}""")]
    [InlineData("""{}""")]
    public async Task GetIngredientsAsync_MissingDinnerSetOrComponents_ReturnsMealNotServedError(string json)
    {
        SetupResponse(HttpStatusCode.OK, json);

        var result = await dinnerProvider.GetIngredientsAsync(new TimeOnly(21, 0), CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error.Kind.ShouldBe(ProviderErrorKind.MealNotServed);
        result.Error.MealType.ShouldBe(MealType.Dinner);
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
