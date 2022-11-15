using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Occasus.Options;
using Occasus.Repository.Interfaces;
using System.Text;

namespace Occasus.JSONRepository
{
    public static class WebApplicationBuilderExtensions
    {
        public static IOptionsStorageRepository UseOptionsFromJsonFile(this WebApplicationBuilder builder, string filePath, Action<JsonSourceSettings>? jsonSourceSettings = null)
            => builder.Services.UseOptionsFromJsonFile(builder.Configuration, filePath, jsonSourceSettings);

        public static IOptionsStorageRepository UseOptionsFromJsonFile(this IServiceCollection services, IConfigurationBuilder configuration, string filePath, Action<JsonSourceSettings>? jsonSourceSettings = null)
        {
            var storageRepository = new JSONSettingsRepository(services, filePath, jsonSourceSettings);

            if (!File.Exists(filePath))
            {
                CreateEmptyJsonFile(filePath);
            }

            storageRepository.AddConfigurationSource(services, configuration);

            services.AddConfigurationSource(storageRepository);

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
