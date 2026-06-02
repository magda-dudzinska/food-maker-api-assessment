using FoodMaker.Domain;
using FoodMaker.Providers;
using FoodMaker.Providers.Results;
using FoodMaker.Services;
using NSubstitute;
using Shouldly;
using Xunit;

namespace FoodMaker.Tests.Services;

public class MealServiceTests
{
    private readonly IMealProvider breakfastProvider = Substitute.For<IMealProvider>();
    private readonly IMealProvider lunchProvider = Substitute.For<IMealProvider>();
    private readonly MealService mealService;

    public MealServiceTests()
    {
        breakfastProvider.MealType.Returns(MealType.Breakfast);
        lunchProvider.MealType.Returns(MealType.Lunch);
        mealService = new MealService(new[] { breakfastProvider, lunchProvider });
    }

    [Fact]
    public async Task GetIngredientsAsync_WhenMealTypeIsBreakfast_CallsBreakfastProviderAndReturnsIngredients()
    {
        var expectedResult = ProviderResult.Success(new List<string> { "bread", "eggs" });
        breakfastProvider
            .GetIngredientsAsync(Arg.Any<TimeOnly>(), Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        var result = await mealService.GetIngredientsAsync(MealType.Breakfast, new TimeOnly(8, 0), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Ingredients.ShouldBe(new[] { "bread", "eggs" });
    }

    [Fact]
    public async Task GetIngredientsAsync_WhenMealTypeIsLunch_CallsLunchProviderAndReturnsIngredients()
    {
        var expectedResult = ProviderResult.Success(new List<string> { "soup", "salad" });
        lunchProvider
            .GetIngredientsAsync(Arg.Any<TimeOnly>(), Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        var result = await mealService.GetIngredientsAsync(MealType.Lunch, new TimeOnly(12, 0), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Ingredients.ShouldBe(new[] { "soup", "salad" });
    }

    [Fact]
    public async Task GetIngredientsAsync_WhenProviderReturnsMealNotServed_PropagatesFailureResult()
    {
        var failureResult = ProviderResult.Failure(new ProviderError(
            ProviderErrorKind.MealNotServed,
            "Breakfast is not served at 23:00",
            MealType.Breakfast,
            new TimeOnly(23, 0)));
        breakfastProvider
            .GetIngredientsAsync(Arg.Any<TimeOnly>(), Arg.Any<CancellationToken>())
            .Returns(failureResult);

        var result = await mealService.GetIngredientsAsync(MealType.Breakfast, new TimeOnly(23, 0), CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error.Kind.ShouldBe(ProviderErrorKind.MealNotServed);
    }

    [Fact]
    public async Task GetIngredientsAsync_WhenProviderReturnsProviderUnavailable_PropagatesFailureResult()
    {
        var failureResult = ProviderResult.Failure(new ProviderError(
            ProviderErrorKind.ProviderUnavailable,
            "Lunch is unavailable",
            MealType.Lunch));
        lunchProvider
            .GetIngredientsAsync(Arg.Any<TimeOnly>(), Arg.Any<CancellationToken>())
            .Returns(failureResult);

        var result = await mealService.GetIngredientsAsync(MealType.Lunch, new TimeOnly(12, 0), CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error.Kind.ShouldBe(ProviderErrorKind.ProviderUnavailable);
    }

    [Fact]
    public async Task GetIngredientsAsync_WhenNoProviderMatchesMealType_ReturnsUnsupportedMealTypeError()
    {
        var result = await mealService.GetIngredientsAsync(MealType.Dinner, new TimeOnly(20, 0), CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error.Kind.ShouldBe(ProviderErrorKind.UnsupportedMealType);
        result.Error.MealType.ShouldBe(MealType.Dinner);
    }

    [Fact]
    public async Task GetIngredientsAsync_WhenTokenIsCancelled_ThrowsOperationCanceledException()
    {
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        await Should.ThrowAsync<OperationCanceledException>(
            () => mealService.GetIngredientsAsync(MealType.Breakfast, new TimeOnly(8, 0), cancellationTokenSource.Token));
    }

    [Fact]
    public async Task GetIngredientsAsync_WhenMealTypeIsBreakfast_DoesNotCallLunchProvider()
    {
        breakfastProvider
            .GetIngredientsAsync(Arg.Any<TimeOnly>(), Arg.Any<CancellationToken>())
            .Returns(ProviderResult.Success(new List<string> { "bread" }));

        await mealService.GetIngredientsAsync(MealType.Breakfast, new TimeOnly(8, 0), CancellationToken.None);

        await lunchProvider.DidNotReceive().GetIngredientsAsync(Arg.Any<TimeOnly>(), Arg.Any<CancellationToken>());
    }
}
