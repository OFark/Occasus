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

namespace Occasus.Options;

public static class WebApplicationBuilderExtensions
{

    private static Assembly ThisAssembly => Assembly.GetAssembly(typeof(WebApplicationBuilderExtensions))!;

    private static bool OcassusAssembled;

    private static ILogger? logger = null;

    private static readonly OccasusMessageStore MessageStore = new();

    private static ILogger CreateLogger(IServiceCollection services) => services.BuildServiceProvider().GetRequiredService<ILogger<WebApplicationBuilder>>();

    public static WebApplicationBuilder UseOccasus(this WebApplicationBuilder builder)
    {
        builder.Services.UseOccasus();
        return builder;
    }

    public static IServiceCollection UseOccasus(this IServiceCollection services)
    {
        logger ??= CreateLogger(services);

        logger.LogInformation("Enabling Occasus");

        if (!OcassusAssembled)
        {
            logger.LogInformation("Occasus requires some assembly");
            logger.LogTrace("Adding this assembly to the Razor Pages");
            services.AddRazorPages().PartManager.ApplicationParts.Add(new AssemblyPart(ThisAssembly));
            logger.LogTrace("Adding ServerSideBlazor");
            services.AddServerSideBlazor();

            logger.LogTrace("Adding MudServices with Snackbar bottom center");
            services.AddMudServices(config =>
            {
                config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomCenter;
            });

            logger.LogTrace("Adding Transient ISettingService");
            services.TryAddTransient<ISettingService, SettingService>();
            logger.LogTrace("Adding Transient IPOCOService");
            services.TryAddTransient<IPOCOService, POCOService>();

            services.TryAddSingleton<OccasusMessageStore>(MessageStore);
        }

        logger.LogInformation("Occasus Assembled");
        OcassusAssembled = true;

        return services;
    }

    public static OptionsBuilder<T> AddOptionsBuilder<T>(this IOptionsStorageRepository storageRepository) where T : class, new()
    {
        var services = storageRepository.Services;
        var configuration = storageRepository.Configuration;
        
        logger ??= CreateLogger(services);

        logger.LogInformation("Adding {classname} to the Setting Store", typeof(T).Name);
        SettingsStore.Add<T>(storageRepository);

        return services.AddOptions<T>()
            .Bind(configuration.GetSection(typeof(T).Name));
    }

    public static IOptionsStorageRepository AddOptions<T>(this IOptionsStorageRepository storageRepository) where T : class, new()
    {
        var services = storageRepository.Services;
        var configuration = storageRepository.Configuration;

        logger ??= CreateLogger(services);

        logger.LogInformation("Adding {classname} to the Setting Store", typeof(T).Name);
        SettingsStore.Add<T>(storageRepository);

        services.AddOptions<T>().Bind(configuration.GetSection(typeof(T).Name));

        return storageRepository;
    }

    public static void AddConfigurationSource(this WebApplicationBuilder builder, IOptionsStorageRepository storageRepository, bool bypassRegistration = false)
    {
        builder.Services.AddConfigurationSource(builder.Configuration, storageRepository, bypassRegistration);
    }

    public static void AddConfigurationSource(this IServiceCollection services, IConfigurationBuilder configuration, IOptionsStorageRepository storageRepository, bool bypassRegistration = false)
    {
        logger ??= CreateLogger(services);

        if (!OcassusAssembled) services.UseOccasus();

        var respositoryname = storageRepository.GetType().Name;
        if (!SettingsStore.ActiveRepositories.Contains(storageRepository))
        {
            if (!bypassRegistration)
            {
                logger.LogInformation("Adding {respositoryname} to the Configuration source", respositoryname);
                configuration.Add<OccasusConfigurationSource>(source => source.StorageRepository = storageRepository);
            }
            SettingsStore.ActiveRepositories.Add(storageRepository);
        }
        else
        {
            logger.LogInformation("Skipped adding {respositoryname} to the Configuration Store, this repository is already in the store.", respositoryname);
        }
    }
}
