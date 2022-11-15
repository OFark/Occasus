using Humanizer.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MudBlazor;
using MudBlazor.Services;
using Occasus.Options;
using Occasus.Repository.Interfaces;
using Occasus.Settings;
using Occasus.Settings.Interfaces;
using System.Reflection;
using static System.Collections.Specialized.BitVector32;

namespace Occasus.Options;

public static class WebApplicationBuilderExtensions
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

    public static OptionsBuilder<T> AddOptionsBuilder<T>(this IOptionsStorageRepository storageRepository, IServiceCollection services) where T : class, new()
    {
        logger ??= CreateLogger(services);

        logger?.LogInformation("Adding {classname} to the Setting Store", typeof(T).Name);
        SettingsStore.Add<T>(storageRepository);

        return services.AddOptions<T>()
            .Configure<IConfiguration>((settings, config) =>
            {
                config.GetSection(typeof(T).Name).Bind(settings);
            });
    }

    public static IOptionsStorageRepository AddOptions<T>(this IOptionsStorageRepository storageRepository, IServiceCollection services) where T : class, new()
    {
        logger ??= CreateLogger(services);

        logger?.LogInformation("Adding {classname} to the Setting Store", typeof(T).Name);
        SettingsStore.Add<T>(storageRepository);

        services.AddOptions<T>()
            .Configure<IConfiguration>((settings, config) =>
            {
                config.GetSection(typeof(T).Name).Bind(settings);
            });

        return storageRepository;
    }

    public static void AddOccasusConfiguration(this IConfigurationBuilder configuration, IOptionsStorageRepository storageRepository)
    {
        configuration.Add<OccasusConfigurationSource>(source => source.StorageRepository = storageRepository);
    }
}
