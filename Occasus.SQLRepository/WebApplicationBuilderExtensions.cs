using Microsoft.AspNetCore.Builder;
using Occasus.Options;
using Occasus.Repository.Interfaces;
using System.Data.SqlClient;

namespace Occasus.SQLRepository
{
    public static class WebApplicationBuilderExtensions
    {
        public static IOptionsStorageRepository UseOptionsFromSQL(this WebApplicationBuilder builder, Action<SQLSourceSettings> settings)
        {
            builder.UseOccasus();

            var storageRepository = new SQLSettingsRepository(builder, settings);

            builder.AddConfigurationSource(storageRepository);

            return storageRepository;
        }
    }
}
