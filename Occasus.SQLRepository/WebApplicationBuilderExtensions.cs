using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Occasus.Options;
using Occasus.Repository.Interfaces;

namespace Occasus.SQLRepository
{
    public static class WebApplicationBuilderExtensions
    {
        public static IOptionsStorageRepository UseOptionsFromSQL(this WebApplicationBuilder builder, Action<SQLSourceSettings> settings)
            => builder.Services.UseOptionsFromSQL(builder.Configuration, settings);
        public static IOptionsStorageRepository UseOptionsFromSQL(this IServiceCollection services, IConfigurationBuilder configuration, Action<SQLSourceSettings> settings)
        {
            services.UseOccasus();

            var storageRepository = new SQLSettingsRepository(services, settings);

            storageRepository.AddConfigurationSource(services, configuration);

            return storageRepository;
        }
    }
}
