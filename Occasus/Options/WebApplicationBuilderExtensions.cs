using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.FileProviders;
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

    private static ILogger CreateLogger(WebApplicationBuilder builder) => builder.Services.BuildServiceProvider().GetRequiredService<ILogger<Program>>();

    public static WebApplicationBuilder UseOccasus(this WebApplicationBuilder builder)
    {
        logger ??= CreateLogger(builder);

        logger.LogInformation("Enabling Occasus");

        if (!OcassusAssembled)
        {
            logger.LogInformation("Occasus requires some assembly");
            logger.LogTrace("Adding this assembly to the Razor Pages");
            builder.Services.AddRazorPages().PartManager.ApplicationParts.Add(new AssemblyPart(ThisAssembly));
            logger.LogTrace("Adding ServerSideBlazor");
            builder.Services.AddServerSideBlazor();

            logger.LogTrace("Adding MudServices with Snackbar bottom center");
            builder.Services.AddMudServices(config =>
            {
                config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomCenter;
            });

            logger.LogTrace("Adding Transient ISettingService");
            builder.Services.AddTransient<ISettingService, SettingService>();
            logger.LogTrace("Adding Transient IPOCOService");
            builder.Services.AddTransient<IPOCOService, POCOService>();
        }

        logger.LogInformation("Occasus Assembled");
        OcassusAssembled = true;

        return builder;
    }

    public static OptionsBuilder<T> AddOptionsBuilder<T>(this IOptionsStorageRepository storageRepository) where T : class, new()
    {
        var builder = storageRepository.Builder;
        
        logger ??= CreateLogger(builder);

        logger.LogInformation("Adding {classname} to the Setting Store", typeof(T).Name);
        SettingsStore.Add<T>(storageRepository);

        return builder.Services.AddOptions<T>()
            .Bind(builder.Configuration.GetSection(typeof(T).Name));
    }

    public static IOptionsStorageRepository AddOptions<T>(this IOptionsStorageRepository storageRepository) where T : class, new()
    {
        var builder = storageRepository.Builder;

        logger ??= CreateLogger(builder);

        logger.LogInformation("Adding {classname} to the Setting Store", typeof(T).Name);
        SettingsStore.Add<T>(storageRepository);

        builder.Services.AddOptions<T>().Bind(builder.Configuration.GetSection(typeof(T).Name));

        return storageRepository;
    }

    public static void AddConfigurationSource(this WebApplicationBuilder builder, IOptionsStorageRepository storageRepository, bool bypassRegistration = false)
    {
        logger ??= CreateLogger(builder);

        if (!OcassusAssembled) builder.UseOccasus();

        var respositoryname = storageRepository.GetType().Name;
        if (!SettingsStore.ActiveRepositories.Contains(storageRepository))
        {
            if (!bypassRegistration)
            {
                logger.LogInformation("Adding {respositoryname} to the Configuration source", respositoryname);
                builder.Configuration.Add<OccasusConfigurationSource>(source => source.StorageRepository = storageRepository);
            }
            SettingsStore.ActiveRepositories.Add(storageRepository);
        }
        else
        {
            logger.LogInformation("Skipped adding {respositoryname} to the Configuration Store, this repository is already in the store.", respositoryname);
        }
    }


    public static void UseOccasusUI(this WebApplication app, string? uiPassword = null)
    {
        logger?.LogInformation("Enabling the Ocassus UI requirements");

        logger?.LogTrace("Adding the Static files from the Occasus Assembly");
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new EmbeddedFileProvider(ThisAssembly, "Occasus.wwwroot")
        });

        if (!string.IsNullOrWhiteSpace(uiPassword))
        {
            app.Configuration["OccasusUI:Password"] = uiPassword;
        }

        app.UseStaticFiles();

        logger?.LogTrace("Map Blazor Hub");
        app.MapBlazorHub();
        logger?.LogTrace("Map Fallback to /_Host");
        app.MapFallbackToPage("/occasus/**", "/_Host");
        logger?.LogTrace("Use Routing");
        app.UseRouting();
    }

}
