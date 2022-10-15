using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Occasus.Options;
using Occasus.Repository.Interfaces;
using System.Text;

namespace Occasus.JSONRepository
{
    public static class WebApplicationBuilderExtensions
    {
        public static IOptionsStorageRepository UseOptionsFromJsonFile(this WebApplicationBuilder builder, string filePath, Action<JsonSourceSettings>? jsonSourceSettings = null)
        {
            var storageRepository = new JSONSettingsRepository(builder, filePath, jsonSourceSettings);

            if(!File.Exists(filePath))
            {
                CreateEmptyJsonFile(filePath);
            }

            builder.Configuration.AddJsonFile(filePath);

            builder.AddConfigurationSource(storageRepository, true);

            return storageRepository;
        }

        private static void CreateEmptyJsonFile(string filePath)
        {
            var fileInfo = new FileInfo(filePath);

            if(fileInfo.Directory is not null && !fileInfo.Directory.Exists)
            {
                fileInfo.Directory.Create();
            }

            File.WriteAllText(filePath, "{}", Encoding.UTF8);
        }
    }
}
