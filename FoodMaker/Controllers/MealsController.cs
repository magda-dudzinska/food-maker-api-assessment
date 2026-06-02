using FoodMaker.Domain;
using FoodMaker.Providers.Results;
using FoodMaker.Services;
using Microsoft.AspNetCore.Mvc;

namespace FoodMaker.Controllers;

[ApiController]
[Route("[controller]")]
public class MealsController : ControllerBase
{
    private readonly MealService mealService;

    public MealsController(MealService mealService)
    {
        this.mealService = mealService;
    }

    [HttpGet("{mealType}")]
    public async Task<IActionResult> GetIngredients(
        string mealType,
        [FromQuery(Name = "time")] string? time,
        CancellationToken cancellationToken)
    {
        if (!MealTypeParser.TryParse(mealType, out var parsedMealType))
        {
            return BadRequest(new { error = $"Unsupported meal type: '{mealType}'" });
        }

        TimeOnly parsedTime;
        if (time is null)
        {
            parsedTime = TimeOnly.FromDateTime(DateTime.Now);
        }
        else
        {
            if (!TimeOnly.TryParse(time, out parsedTime))
            {
                return BadRequest(new { error = $"Invalid time format: '{time}'" });
            }
        }

        var result = await mealService.GetIngredientsAsync(parsedMealType, parsedTime, cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(result.Ingredients);
        }

        return result.Error!.Kind switch
        {
            ProviderErrorKind.MealNotServed => NotFound(new { error = result.Error.Message }),
            ProviderErrorKind.ProviderUnavailable => StatusCode(502, new { error = result.Error.Message }),
            ProviderErrorKind.UnsupportedMealType => BadRequest(new { error = result.Error.Message }),
            _ => StatusCode(500)
        };
    }
}
