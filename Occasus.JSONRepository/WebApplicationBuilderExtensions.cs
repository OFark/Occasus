using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Occasus.Options;
using Occasus.Repository.Interfaces;

namespace Occasus.JSONRepository
{
    public static class WebApplicationBuilderExtensions
    {
        public static IOptionsStorageRepository UseOptionsFromJsonFile(this WebApplicationBuilder builder, string filePath, Action<JsonSourceSettings>? jsonSourceSettings = null)
        {
            var storageRepository = new JSONSettingsRepository(builder, filePath, jsonSourceSettings);

            builder.Configuration.AddJsonFile(filePath);

            builder.AddConfigurationSource(storageRepository, true);

            return storageRepository;
        }
    }
}
