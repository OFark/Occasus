using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.FileProviders;
using MudBlazor;
using MudBlazor.Services;
using Occasus.Options;
using System.Reflection;

namespace Occasus.BlazorUI;

public static class Extensions
{

    private static Assembly ThisAssembly => Assembly.GetAssembly(typeof(Extensions))!;

    private static ILogger? CreateLogger(IServiceProvider services) => services.GetService<ILogger<Program>>();


    public static WebApplicationBuilder AddOccasusUI(this WebApplicationBuilder builder)
    {
        builder.Services.AddOccasusUI();
        return builder;
    }

    public static IServiceCollection AddOccasusUI(this IServiceCollection services)
    {
        var logger = services.BuildServiceProvider().GetService<ILogger<Program>>();

        logger?.LogInformation("Occasus requires some assembly");
        logger?.LogTrace("Adding this assembly to the Razor Pages");
        services.AddRazorPages().PartManager.ApplicationParts.Add(new AssemblyPart(ThisAssembly));
        logger?.LogTrace("Adding ServerSideBlazor");
        services.AddServerSideBlazor();

        logger?.LogTrace("Adding MudServices with Snackbar bottom center");
        services.AddMudServices(config =>
        {
            config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomCenter;
        });

        services.AddOccasusUIServices();

        return services;
    }


    public static void UseOccasusUI(this WebApplication app, string? uiPassword = null, string uiLocation = "/occasus")
    {
        var logger = CreateLogger(app.Services);

        logger?.LogInformation("Enabling the Ocassus UI requirements");


        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new EmbeddedFileProvider(ThisAssembly, "Occasus.BlazorUI.wwwroot"),
            RequestPath = uiLocation
        });

        app.UseStaticFiles(uiLocation);

        app.Configuration["OccasusUI:Location"] = uiLocation;

        if (!string.IsNullOrWhiteSpace(uiPassword))
        {
            app.Configuration["OccasusUI:Password"] = uiPassword;
        }

        app.MapFallbackToPage($"{uiLocation}/{{*path:nonfile}}", "/_OccasusHost");

        app.UseRouting();

        app.UseStaticFiles();

        app.MapBlazorHub($"{uiLocation}/_blazor");
    }

}
