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

    /// <summary>
    /// Returns the list of ingredients for a given meal type at the specified time.
    /// </summary>
    /// <param name="mealType">The meal type: breakfast, lunch or dinner (case-insensitive).</param>
    /// <param name="recievedTime">Optional serving time in HH:mm format. Defaults to current time.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <remarks>
    /// Sample requests:
    ///
    ///     GET /meals/breakfast?time=08:00
    ///     GET /meals/lunch?time=13:00
    ///     GET /meals/dinner?time=19:00
    ///     GET /meals/lunch - uses current time
    /// </remarks>
    /// <response code="200">Ingredients list for the requested meal.</response>
    /// <response code="400">Invalid meal type or time format.</response>
    /// <response code="404">The requested meal is not served at the given time.</response>
    /// <response code="502">The upstream meal provider is unavailable.</response>
    [ProducesResponseType<IReadOnlyList<string>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    [HttpGet("{mealType}")]
    public async Task<IActionResult> GetIngredients(
        string mealType,
        [FromQuery(Name = "time")] string? recievedTime,
        CancellationToken cancellationToken)
    {
        if (!MealTypeParser.TryParse(mealType, out var parsedMealType))
        {
            return BadRequest(new { error = $"Unsupported meal type: '{mealType}'" });
        }

        TimeOnly mealTime;
        if (recievedTime is null)
        {
            mealTime = TimeOnly.FromDateTime(DateTime.Now);
        }
        else
        {
            if (!TimeOnly.TryParse(recievedTime, out mealTime))
            {
                return BadRequest(new { error = $"Invalid time format: '{recievedTime}'" });
            }
        }

        var result = await mealService.GetIngredientsAsync(parsedMealType, mealTime, cancellationToken);

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
