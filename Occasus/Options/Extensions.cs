using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Occasus.Repository.Interfaces;
using Occasus.Settings;
using Occasus.Settings.Interfaces;

namespace Occasus.Options;

public static class Extensions
{

    private static bool OcassusAssembled;

    private static ILogger? logger = null;

    private static readonly OccasusMessageStore MessageStore = new();

    private static ILogger? CreateLogger(IServiceCollection services) => services.BuildServiceProvider().GetService<ILogger<WebApplicationBuilder>>();

    public static IServiceCollection AddOccasusUIServices(this IServiceCollection services)
    {
        logger ??= CreateLogger(services);

        logger?.LogInformation("Enabling Occasus");

        if (!OcassusAssembled)
        {
            logger?.LogTrace("Adding Transient ISettingService");
            services.TryAddTransient<ISettingService, SettingService>();
            logger?.LogTrace("Adding Transient IPOCOService");
            services.TryAddTransient<IPOCOService, POCOService>();

            services.TryAddSingleton(MessageStore);
        }

        logger?.LogInformation("Occasus Assembled");
        OcassusAssembled = true;

        return services;
    }

    public static IOptionsStorageRepositoryWithServices WithOptions<TOption>(this IOptionsStorageRepositoryWithServices storageRepository) where TOption : class, new()
        => storageRepository.WithOptions<TOption>(out _);

    public static IOptionsStorageRepositoryWithServices WithOptions<TOption>(this IOptionsStorageRepositoryWithServices storageRepository, out OptionsBuilder<TOption> optionBuilder) where TOption : class, new()
    {
        var services = storageRepository.Services;
        logger ??= CreateLogger(services);

        logger?.LogInformation("Adding {classname} to the Setting Store", typeof(TOption).Name);
        SettingsStore.Add<TOption>(storageRepository);

        services.WithOptions(out optionBuilder);

        return storageRepository;
    }

    public static IServiceCollection WithOptions<TOption>(this IServiceCollection services) where TOption : class, new()
        => services.WithOptions<TOption>(out _);

    public static IServiceCollection WithOptions<TOption>(this IServiceCollection services, out OptionsBuilder<TOption> optionBuilder) where TOption : class, new()
    {
        optionBuilder = services.AddOptions<TOption>()
            .Configure<IConfiguration>((settings, config) =>
            {
                config.GetSection(typeof(TOption).Name).Bind(settings);
            });

        return services;
    }

    public static void AddOccasusStorageRepository(this IConfigurationBuilder configuration, IOptionsStorageRepository storageRepository, bool bypassRegistration = false)
    {

        if (SettingsStore.TryAdd(storageRepository) && !bypassRegistration)
        {
            configuration.Add<OccasusConfigurationSource>(source => source.StorageRepository = storageRepository);
        }
    }

}
