using BlazorAppTest.Data;
using Occasus.BlazorUI;
using Occasus.JSONRepository;
using Occasus.Options;
using Occasus.SQLRepository;
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
    .WithOptions<TestDictionaries>();

builder.UseOptionsFromJsonFile("appsettings.json", settings =>
{
    settings.JsonWriterOptions((ref JsonWriterOptions options) => options.Indented = true);
    settings.JsonNodeOptions((ref JsonNodeOptions options) => options.PropertyNameCaseInsensitive = true);
})
    .WithOptions<TestAppSettingsJson>();

builder.UseOptionsFromJsonFile("settings/settings.json")
    .WithOptions<TestJson>();


// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddSingleton<WeatherForecastService>();

var app = builder.Build();

app.UseOccasusUI();


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}


app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
