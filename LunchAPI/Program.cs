using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

var items12to14 = new
{
    items = new[]
    {
        "soup",
        "potatoes",
        "salad"
    }
};

var items14to17 = new
{
    items = new[]
    {
        "pizza"
    }
};

app.MapGet("/items", ([FromQuery] int hour) =>
{
    if (hour >= 12 && hour < 14)
    {
        return Results.Ok(items12to14);
    }
    else if (hour >= 14 && hour < 17)
    {
        return Results.Ok(items14to17);
    }

    return Results.BadRequest(new { error = "hour must be between 12 and 17" });
});

app.Run();