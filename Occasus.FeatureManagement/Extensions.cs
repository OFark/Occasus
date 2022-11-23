using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement;
using Occasus.Options;
using Occasus.Repository.Interfaces;

namespace Occasus.FeatureManagement
{
    public static class Extensions
    {
        public static IOptionsStorageRepositoryWithServices WithFeatureFlagOptions<TOption>(this IOptionsStorageRepositoryWithServices storageRepository, IConfiguration configuration) where TOption : class, new()
        => storageRepository.WithFeatureFlagOptions<TOption>(configuration, out _);

        public static IOptionsStorageRepositoryWithServices WithFeatureFlagOptions<TOption>(this IOptionsStorageRepositoryWithServices storageRepository, IConfiguration configuration, out OptionsBuilder<TOption> optionBuilder) where TOption : class, new()
        {
            var services = storageRepository.Services;
            services.AddFeatureManagement(configuration.GetSection(typeof(TOption).Name));

            return storageRepository.WithOptions(out optionBuilder); ;
        }

        public static IServiceCollection WithFeatureFlagOptions<TOption>(this IServiceCollection services, IConfiguration configuration) where TOption : class, new()
            => services.WithFeatureFlagOptions<TOption>(configuration, out _);

        public static IServiceCollection WithFeatureFlagOptions<TOption>(this IServiceCollection services, IConfiguration configuration, out OptionsBuilder<TOption> optionBuilder) where TOption : class, new()
        {
            services.AddFeatureManagement(configuration.GetSection(typeof(TOption).Name));

            return services.WithOptions(out optionBuilder);
        }
    }
}