using FoodMaker.Domain;
using Shouldly;
using Xunit;

namespace FoodMaker.Tests.Domain;

public class MealTypeParserTests
{
    [Theory]
    [InlineData("Breakfast", MealType.Breakfast)]
    [InlineData("lunch", MealType.Lunch)]
    [InlineData("DINNER", MealType.Dinner)]
    public void TryParse_WithValidMealType_ReturnsTrueAndParsedValue(string input, MealType expectedMealType)
    {
        var success = MealTypeParser.TryParse(input, out var result);

        success.ShouldBeTrue();
        result.ShouldBe(expectedMealType);
    }

    [Theory]
    [InlineData("Brunch")]
    [InlineData("")]
    [InlineData("123")]
    public void TryParse_WithInvalidInput_ReturnsFalse(string input)
    {
        var success = MealTypeParser.TryParse(input, out _);

        success.ShouldBeFalse();
    }
}
