using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Occasus.Repository.Interfaces;
using System.Text;

namespace Occasus.JSONRepository
{
    public static class WebApplicationBuilderExtensions
    {
        public static IOptionsStorageRepository UseOptionsFromJsonFile(this WebApplicationBuilder builder, string filePath, Action<JsonSourceSettings>? jsonSourceSettings = null)
            => builder.Configuration.UseOptionsFromJsonFile(filePath, jsonSourceSettings);

        public static IOptionsStorageRepository UseOptionsFromJsonFile(this IConfigurationBuilder configuration, string filePath, Action<JsonSourceSettings>? jsonSourceSettings = null)
        {
            if (!File.Exists(filePath))
            {
                CreateEmptyJsonFile(filePath);
            }

            var storageRepository = new JSONSettingsRepository(filePath, jsonSourceSettings);

            storageRepository.AddConfigurationSource(configuration);

            return storageRepository;
        }

        private static void CreateEmptyJsonFile(string filePath)
        {
            var fileInfo = new FileInfo(filePath);

            if (fileInfo.Directory is not null && !fileInfo.Directory.Exists)
            {
                fileInfo.Directory.Create();
            }

            File.WriteAllText(filePath, "{}", Encoding.UTF8);
        }
    }
}
