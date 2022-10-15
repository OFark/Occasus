using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using Occasus.Repository.Interfaces;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Configuration;

namespace Occasus.JSONRepository
{
    public class JSONSettingsRepository : IOptionsStorageRepository
    {
        private readonly string filePath;
        private readonly JsonSourceSettings jsonSourceSettings;
        private CancellationTokenSource? changeCancellationTokenSource;
        private IChangeToken? changeToken;

        public JSONSettingsRepository(WebApplicationBuilder builder, string filePath, Action<JsonSourceSettings>? jsonSourceSettings = null)
        {
            Builder = builder;
            this.filePath = filePath;
            this.jsonSourceSettings = new(filePath);
            jsonSourceSettings?.Invoke(this.jsonSourceSettings);
        }

        public WebApplicationBuilder Builder { get; }
        public async Task ClearSettings(string? classname = null, CancellationToken cancellation = default)
        {
            if (!File.Exists(filePath))
            {
                return;
            }

            using var fileStream = new FileStream(filePath, FileMode.Open);

            var root = JsonNode.Parse(fileStream, jsonSourceSettings.NodeOptions, jsonSourceSettings.DocumentOptions);

            if (root is null)
            {
                return;
            }

            if (classname is not null)
            {
                if (!root.AsObject().ContainsKey(classname))
                {
                    return;
                }

                root.AsObject().Remove(classname);

                //Builder.Configuration.GetSection(classname).GetReloadToken();

            }

            if (classname is null && jsonSourceSettings.ClearWholeFile)
            {
                root.AsObject().Clear();
            }

            fileStream.Position = 0;
            fileStream.SetLength(0);

            using var writer = new Utf8JsonWriter(fileStream, jsonSourceSettings.WriterOptions);
            root.WriteTo(writer, jsonSourceSettings.SerializerOptions);

            await fileStream.FlushAsync(cancellation).ConfigureAwait(false);
        }

        public IDictionary<string, string> LoadSettings()
        {
            return new Dictionary<string, string>();
        }

        public Task ReloadSettings(CancellationToken cancellation = default)
        {
            ((IConfigurationRoot)Builder.Configuration).Reload();
            return Task.CompletedTask;
        }

        public async Task StoreSetting<T>(T value, Type valueType, CancellationToken cancellation = default)
        {
            using var fileStream = new FileStream(filePath, FileMode.OpenOrCreate);

            var root = JsonNode.Parse(fileStream, jsonSourceSettings.NodeOptions, jsonSourceSettings.DocumentOptions);

            if (root is null)
            {
                root = JsonNode.Parse("{}");
            }

            if(root is null) throw new Exception("Json parsing failure");

            var className = valueType.Name;

            root.AsObject().Remove(className);

            var memoryStream = new MemoryStream();
            await JsonSerializer.SerializeAsync(memoryStream, value, jsonSourceSettings.SerializerOptions, cancellation).ConfigureAwait(false);

            memoryStream.Position = 0;

            var valueNode = JsonNode.Parse(memoryStream, jsonSourceSettings.NodeOptions);

            root.AsObject().Add(className, valueNode);

            fileStream.Position = 0;
            fileStream.SetLength(0);

            using var writer = new Utf8JsonWriter(fileStream, jsonSourceSettings.WriterOptions);
            root.WriteTo(writer, jsonSourceSettings.SerializerOptions);

            await fileStream.FlushAsync(cancellation).ConfigureAwait(false);
        }

        public IChangeToken Watch() => ((IConfigurationRoot)Builder.Configuration).GetReloadToken();
    }
}