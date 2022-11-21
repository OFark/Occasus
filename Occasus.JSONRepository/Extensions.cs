using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Occasus.Options;
using Occasus.Repository.Interfaces;
using System.Text;

namespace Occasus.JSONRepository
{
    public static class Extensions
    {
        public static IOptionsStorageRepositoryWithServices UseOptionsFromJsonFile(this WebApplicationBuilder builder, string filePath, Action<JsonSourceSettings>? jsonSourceSettings = null)
            => builder.Configuration.UseOptionsFromJsonFile(filePath, jsonSourceSettings).AddServices(builder.Services);

        public static IOptionsStorageRepository UseOptionsFromJsonFile(this IConfigurationBuilder configuration, string filePath, Action<JsonSourceSettings>? jsonSourceSettings = null)
        {
            var storageRepository = new JSONSettingsRepository(filePath, jsonSourceSettings);

            configuration.AddOccasusStorageRepository(storageRepository, true);

            configuration.AddJsonFile(filePath, false, true);

            return storageRepository;
        }
    }
}
