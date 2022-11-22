using Occasus.JSONRepository;
using Occasus.Options;
using Occasus.SQLRepository;
using Occasus.BlazorUI;
using System.Text.Json;
using System.Text.Json.Nodes;
using TestClassLibrary.TestModels;

var builder = WebApplication.CreateBuilder(args);

builder.AddOccasusUI()

.UseOptionsFromSQL(settings =>
{
    settings.EncryptSettings = true;
    settings.EncryptionKey = "mypassword";
    settings.WithSQLConnection(sqlConnBuilder =>
    {
        sqlConnBuilder.ConnectionString = builder.Configuration["ConnectionStrings:SettingsConnectionString"];
        sqlConnBuilder.PersistSecurityInfo = true;
        //sqlConnBuilder.MultipleActiveResultSets = true;
        //sqlConnBuilder.IntegratedSecurity = true;
    });
})
    .WithOptions<TestSimple>()
    .WithOptions<TestComplex>()
    .WithOptions<TestArrays>()
    .WithOptions<TestHashSets>()
    .WithOptions<TestLists>()
    .WithOptions<TestDictionaries>(out var optionsBuilder);

optionsBuilder.Validate(x => x.TestDictionaryStringStrings != null && x.TestDictionaryStringStrings.Any(), "Test DictionaryStrings must have some value");


builder.UseOptionsFromJsonFile("appsettings.json", settings =>
{
    settings.JsonWriterOptions((ref JsonWriterOptions options) => options.Indented = true);
    settings.JsonNodeOptions((ref JsonNodeOptions options) => options.PropertyNameCaseInsensitive = true);
})
    .WithOptions<TestAppSettingsJson>();

builder.UseOptionsFromJsonFile("settings/settings.json")
    .WithOptions<TestJson>();

// Add services to the container.

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseOccasusUI("mypassword");

object? state = default;

app.Configuration.GetSection("TestSimple").GetReloadToken().RegisterChangeCallback((x) => Console.WriteLine("TestSImple has changed"), state);


var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateTime.Now.AddDays(index),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
});


app.Run();

internal record WeatherForecast(DateTime Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}