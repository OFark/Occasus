using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Occasus.BlazorUI;
using Occasus.FeatureManagement;
using Occasus.JSONRepository;
using Occasus.Options;
using Occasus.SQLEFRepository;
using System.Text.Json;
using System.Text.Json.Nodes;
using TestClassLibrary.TestModels;

var builder = WebApplication.CreateBuilder(args);

builder.AddOccasusUI()

.UseOptionsFromSQLEF(settings =>
{
    settings.EncryptSettings = true;
    settings.EncryptionKey = "mypasswordneedstobelonger";
    settings.WithSQLConnection(sqlConnBuilder =>
    {
        sqlConnBuilder.ConnectionString = builder.Configuration["ConnectionStrings:SettingsConnectionString"];
        sqlConnBuilder.PersistSecurityInfo = true;
        //sqlConnBuilder.MultipleActiveResultSets = true;
        //sqlConnBuilder.IntegratedSecurity = true;
    }, dbOptions =>
    {
        dbOptions.EnableRetryOnFailure(3, new(0, 0, 5), null);
    });
})
    .WithOptions<TestSimple>()
    .WithOptions<TestComplex>()
    .WithOptions<TestArrays>()
    .WithOptions<TestHashSets>()
    .WithOptions<TestLists>()
    .WithFeatureFlagOptions<TestSimple>(builder.Configuration)
    .WithOptions<TestDictionaries>(out var optionsBuilder);

optionsBuilder.Validate(x => x.TestDictionaryStringStrings != null && x.TestDictionaryStringStrings.Count != 0, "Test DictionaryStrings must have some value");


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

app.UseOccasusUI("mypassword", "/other");



ChangeToken.OnChange(() => app.Configuration.GetSection("TestSimple").GetReloadToken(), delegate
{
    Console.WriteLine("Something has changed");
});


app.MapGet("/testSimple", (IOptionsSnapshot<TestSimple> testSimple) =>
{

    return testSimple.Value;
});


app.Run();
