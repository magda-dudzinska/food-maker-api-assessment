using FoodMaker.Domain;
using FoodMaker.Providers;
using FoodMaker.Providers.Results;
using Microsoft.Extensions.Caching.Memory;
using NSubstitute;
using Shouldly;
using Xunit;

namespace FoodMaker.Tests.Providers;

public class CachedMealProviderTests
{
    private readonly IMealProvider innerProvider = Substitute.For<IMealProvider>();
    private readonly IMemoryCache cache = new MemoryCache(new MemoryCacheOptions());
    private readonly CachedMealProvider cachedMealProvider;

    public CachedMealProviderTests()
    {
        innerProvider.MealType.Returns(MealType.Breakfast);
        cachedMealProvider = new CachedMealProvider(innerProvider, cache);
    }

    [Fact]
    public async Task GetIngredientsAsync_FirstCall_DelegatesToInnerProvider()
    {
        var expectedResult = ProviderResult.Success(new List<string> { "bread", "eggs" });
        innerProvider
            .GetIngredientsAsync(Arg.Any<TimeOnly>(), Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        var result = await cachedMealProvider.GetIngredientsAsync(new TimeOnly(8, 0), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Ingredients.ShouldBe(new[] { "bread", "eggs" });
        await innerProvider.Received(1).GetIngredientsAsync(new TimeOnly(8, 0), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetIngredientsAsync_SecondCallSameKey_ReturnsCachedResultWithoutCallingInnerProvider()
    {
        var expectedResult = ProviderResult.Success(new List<string> { "bread", "eggs" });
        innerProvider
            .GetIngredientsAsync(Arg.Any<TimeOnly>(), Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        await cachedMealProvider.GetIngredientsAsync(new TimeOnly(8, 0), CancellationToken.None);
        var result = await cachedMealProvider.GetIngredientsAsync(new TimeOnly(8, 0), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Ingredients.ShouldBe(new[] { "bread", "eggs" });
        await innerProvider.Received(1).GetIngredientsAsync(new TimeOnly(8, 0), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetIngredientsAsync_DifferentTimes_CallsInnerProviderForEach()
    {
        innerProvider
            .GetIngredientsAsync(new TimeOnly(8, 0), Arg.Any<CancellationToken>())
            .Returns(ProviderResult.Success(new List<string> { "bread" }));
        innerProvider
            .GetIngredientsAsync(new TimeOnly(9, 0), Arg.Any<CancellationToken>())
            .Returns(ProviderResult.Success(new List<string> { "pancakes" }));

        var firstResult = await cachedMealProvider.GetIngredientsAsync(new TimeOnly(8, 0), CancellationToken.None);
        var secondResult = await cachedMealProvider.GetIngredientsAsync(new TimeOnly(9, 0), CancellationToken.None);

        firstResult.Ingredients.ShouldBe(new[] { "bread" });
        secondResult.Ingredients.ShouldBe(new[] { "pancakes" });
        await innerProvider.Received(1).GetIngredientsAsync(new TimeOnly(8, 0), Arg.Any<CancellationToken>());
        await innerProvider.Received(1).GetIngredientsAsync(new TimeOnly(9, 0), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetIngredientsAsync_FailureResult_IsNotCachedAndDelegatesAgain()
    {
        var failureResult = ProviderResult.Failure(new ProviderError(
            ProviderErrorKind.ProviderUnavailable,
            "Breakfast is unavailable",
            MealType.Breakfast));
        var successResult = ProviderResult.Success(new List<string> { "bread" });

        innerProvider
            .GetIngredientsAsync(new TimeOnly(8, 0), Arg.Any<CancellationToken>())
            .Returns(failureResult, successResult);

        var firstResult = await cachedMealProvider.GetIngredientsAsync(new TimeOnly(8, 0), CancellationToken.None);
        var secondResult = await cachedMealProvider.GetIngredientsAsync(new TimeOnly(8, 0), CancellationToken.None);

        firstResult.IsSuccess.ShouldBeFalse();
        secondResult.IsSuccess.ShouldBeTrue();
        secondResult.Ingredients.ShouldBe(new[] { "bread" });
        await innerProvider.Received(2).GetIngredientsAsync(new TimeOnly(8, 0), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void MealType_ReturnsInnerProviderMealType()
    {
        cachedMealProvider.MealType.ShouldBe(MealType.Breakfast);
    }

    [Fact]
    public async Task GetIngredientsAsync_WhenTokenIsCancelled_ThrowsAndDoesNotCache()
    {
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        await Should.ThrowAsync<OperationCanceledException>(
            () => cachedMealProvider.GetIngredientsAsync(new TimeOnly(8, 0), cancellationTokenSource.Token));

        await innerProvider.DidNotReceive().GetIngredientsAsync(Arg.Any<TimeOnly>(), Arg.Any<CancellationToken>());
    }
}
