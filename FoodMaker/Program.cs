using FoodMaker.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMemoryCache();
builder.Services.AddMealProviders(builder.Configuration);
builder.Services.AddMealServices();
builder.Services.AddControllers();

var app = builder.Build();

app.MapControllers();

app.Run();
