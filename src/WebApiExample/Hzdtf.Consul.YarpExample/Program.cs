using Hzdtf.Utility;

var builder = WebApplication.CreateBuilder(args);
App.CurrConfig = builder.Configuration;
builder.Services.AddYarp();

var app = builder.Build();
App.Instance = app.Services;
app.UseYarp(app.Lifetime);

app.MapGet("/", () => "Hello World!");

app.Run();
