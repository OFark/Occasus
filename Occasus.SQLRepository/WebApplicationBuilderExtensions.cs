using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Occasus.Repository.Interfaces;

namespace Occasus.SQLRepository
{
    public static class WebApplicationBuilderExtensions
    {
        public static IOptionsStorageRepository UseOptionsFromSQL(this WebApplicationBuilder builder, Action<SQLSourceSettings> settings)
            => builder.Configuration.UseOptionsFromSQL(settings);

        public static IOptionsStorageRepository UseOptionsFromSQL(this IConfigurationBuilder configuration, Action<SQLSourceSettings> settings)
        {
            var storageRepository = new SQLSettingsRepository(settings);

            storageRepository.AddConfigurationSource(configuration);

            return storageRepository;
        }
    }
}
