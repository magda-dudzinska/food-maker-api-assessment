namespace FoodMaker.Domain;

public static class MealTypeParser
{
    public static bool TryParse(string input, out MealType result) =>
        Enum.TryParse(input, ignoreCase: true, out result) && Enum.IsDefined(result);
}
