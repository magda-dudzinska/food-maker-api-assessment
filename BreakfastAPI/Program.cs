using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

var products7to9 = new
{
    products = new[]
    {
        new { name = "bread" },
        new { name = "eggs" },
        new { name = "onion" }
    }
};

var products9to11 = new
{
    products = new[]
    {
        new { name = "bread" },
        new { name = "cheese" },
        new { name = "tomato" }
    }
};

app.MapGet("/ingridients", ([FromQuery] TimeOnly time) =>
{
    if (time.Hour >= 7 && time.Hour < 9)
    {
        return Results.Ok(products7to9);
    }
    else if (time.Hour >= 9 && time.Hour < 11)
    {
        return Results.Ok(products9to11);
    }

    return Results.Ok(new { products = Array.Empty<object>() });    
});

app.Run();