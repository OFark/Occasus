using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Occasus.Options;
using Occasus.Repository.Interfaces;

namespace Occasus.SQLRepository
{
    public static class Extensions
    {
        public static IOptionsStorageRepositoryWithServices UseOptionsFromSQL(this WebApplicationBuilder builder, Action<SQLSourceSettings> settings)
            => builder.Configuration.UseOptionsFromSQL(settings).AddServices(builder.Services);
        

        public static IOptionsStorageRepository UseOptionsFromSQL(this IConfigurationBuilder configuration, Action<SQLSourceSettings> settings)
        {
            var storageRepository = new SQLSettingsRepository(settings);

            configuration.AddOccasusStorageRepository(storageRepository);

            return storageRepository;
        }
    }
}
