﻿using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Occasus.Options;
using Occasus.Repository.Interfaces;
using Occasus.SQLEFRepository.Models;

namespace Occasus.SQLEFRepository
{
    public static class Extensions
    {

        public static IOptionsStorageRepositoryWithServices UseOptionsFromSQLEF(this WebApplicationBuilder builder, Action<SQLEFSourceSettings> settings)
            => builder.Configuration.UseOptionsFromSQLEF(settings).AddServices(builder.Services);

        public static IOptionsStorageRepositoryWithServices UseOptionsFromSQLEF(this WebApplicationBuilder builder, Action<SQLEFSourceSettings> settings, Action<DbContextOptionsBuilder<OccasusContext>> context)
            => builder.Configuration.UseOptionsFromSQLEF(settings, context).AddServices(builder.Services);


        public static IOptionsStorageRepository UseOptionsFromSQLEF(this IConfigurationBuilder configuration, Action<SQLEFSourceSettings> settings)
        {
            var storageRepository = new SQLEFSettingsRepository(settings);

            configuration.AddOccasusStorageRepository(storageRepository);

            return storageRepository;
        }

        public static IOptionsStorageRepository UseOptionsFromSQLEF(this IConfigurationBuilder configuration, Action<SQLEFSourceSettings> settings, Action<DbContextOptionsBuilder<OccasusContext>> context)
        {
            var storageRepository = new SQLEFSettingsRepository(settings, context);

            configuration.AddOccasusStorageRepository(storageRepository);

            return storageRepository;
        }

        public static IServiceProvider CreateOccasusDb(this IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var occasusContext = scope.ServiceProvider.GetRequiredService<OccasusContext>();

            occasusContext.Database.EnsureCreated();

            return services;
        }

        public async static Task<IServiceProvider> CreateOccasusDbAsync(this IServiceProvider services, CancellationToken cancellation = default)
        {
            using var scope = services.CreateScope();
            var occasusContext = scope.ServiceProvider.GetRequiredService<OccasusContext>();

            await occasusContext.Database.EnsureCreatedAsync(cancellation).ConfigureAwait(false);

            return services;
        }
    }
}
