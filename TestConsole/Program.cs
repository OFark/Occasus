using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Occasus.JSONRepository;
using Occasus.Options;
using Occasus.SQLRepository;
using System.Reflection;
using System.Text.Json;
using TestClassLibrary.TestModels;

namespace TestConsole
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Host.CreateDefaultBuilder(args)
                .ConfigureHostConfiguration(configure =>
                {
                    configure.AddUserSecrets(Assembly.GetAssembly(typeof(Program))!);
                    configure.UseOptionsFromSQL(settings =>
                    {
                        var configuration = configure.Build();
                        settings.EncryptSettings = true;
                        settings.EncryptionKey = "mypassword";
                        settings.WithSQLConnection(sqlConnBuilder =>
                        {
                            sqlConnBuilder.ConnectionString = configuration["ConnectionStrings:SettingsConnectionString"];
                            sqlConnBuilder.PersistSecurityInfo = true;
                        });
                    });
                    configure.UseOptionsFromJsonFile("settings/settings.json", jsonSourceSettings =>
                    {
                        jsonSourceSettings.ClearWholeFile = true;
                        jsonSourceSettings.JsonWriterOptions((ref JsonWriterOptions options) =>
                        {
                            options.Indented = true;
                        });
                    });
                })
                .ConfigureServices((ctx, services) =>
                {
                    services.WithOptions<TestSimple>()
                            .WithOptions<TestComplex>()
                            .WithOptions<TestArrays>()
                            .WithOptions<TestHashSets>()
                            .WithOptions<TestLists>()
                            .WithOptions<TestDictionaries>()
                            .WithOptions<TestAppSettingsJson>()
                            .WithOptions<TestJson>()

                            .AddSingleton<ConsoleTest, ConsoleTest>()
                            .BuildServiceProvider()
                            .GetRequiredService<ConsoleTest>()
                            .DoTest();
                })
                .Build();
        }
    }

    internal class ConsoleTest
    {
        private readonly TestSimple testSimple;
        private readonly TestComplex testComplex;
        private readonly TestArrays testArrays;
        private readonly TestHashSets testHashSets;
        private readonly TestLists testLists;
        private readonly TestDictionaries testDictionaries;
        private readonly TestAppSettingsJson testAppSettingsJson;
        private readonly TestJson testJson;

        public ConsoleTest(IOptions<TestSimple> testSimple,
                           IOptions<TestComplex> testComplex,
                           IOptions<TestArrays> testArrays,
                           IOptions<TestHashSets> testHashSets,
                           IOptions<TestLists> testLists,
                           IOptions<TestDictionaries> testDictionaries,
                           IOptions<TestAppSettingsJson> testAppSettingsJson,
                           IOptions<TestJson> testJson)
        {
            this.testSimple = testSimple.Value;
            this.testComplex = testComplex.Value;
            this.testArrays = testArrays.Value;
            this.testHashSets = testHashSets.Value;
            this.testLists = testLists.Value;
            this.testDictionaries = testDictionaries.Value;
            this.testAppSettingsJson = testAppSettingsJson.Value;
            this.testJson = testJson.Value;
        }

        internal void DoTest()
        {
            
            Console.WriteLine(JsonSerializer.Serialize(testSimple));
            Console.WriteLine();
            Console.WriteLine(JsonSerializer.Serialize(testComplex));
            Console.WriteLine();
            Console.WriteLine(JsonSerializer.Serialize(testArrays));
            Console.WriteLine();
            Console.WriteLine(JsonSerializer.Serialize(testHashSets));
            Console.WriteLine();
            Console.WriteLine(JsonSerializer.Serialize(testLists));
            Console.WriteLine();
            Console.WriteLine(JsonSerializer.Serialize(testDictionaries));
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine(JsonSerializer.Serialize(testAppSettingsJson));
            Console.WriteLine();
            Console.WriteLine(JsonSerializer.Serialize(testJson));

            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
        }
    }
}