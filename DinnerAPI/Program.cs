using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

var components18to20 = new
{
    dinnerSet = new
    {
        components = new[]
        {
            new { name = "steak" },
            new { name = "rice" },
            new { name = "sauce" }
        }
    }
};

var components20to22 = new
{
    dinnerSet = new
    {
        components = new[]
        {
            new { name = "pasta" },
            new { name = "cheese" },
            new { name = "wine sauce" }
        }
    }
};

app.MapGet("/components/{hour}", (int hour) =>
{
    if (hour >= 18 && hour < 20)
    {
        return Results.Ok(components18to20);
    }
    else if (hour >= 20 && hour < 22)
    {
        return Results.Ok(components20to22);
    }

    return Results.BadRequest(new { error = "hour must be between 18 and 22" });
});

app.Run();