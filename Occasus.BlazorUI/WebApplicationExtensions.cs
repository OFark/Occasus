using Microsoft.Extensions.FileProviders;
using System.Reflection;

namespace Occasus.BlazorUI;

public static class WebApplicationExtensions
{

    private static Assembly ThisAssembly => Assembly.GetAssembly(typeof(WebApplicationExtensions))!;

    private static ILogger? logger = null;
    private static ILogger CreateLogger(IServiceProvider services) => services.GetRequiredService<ILogger<Program>>();


    public static void UseOccasusUI(this WebApplication app, string? uiPassword = null)
    {
        logger ??= CreateLogger(app.Services);

        logger?.LogInformation("Enabling the Ocassus UI requirements");

        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new EmbeddedFileProvider(ThisAssembly, "Occasus.wwwroot"),
            RequestPath = "/occasus"
        });

        app.UseStaticFiles("/occasus");


        if (!string.IsNullOrWhiteSpace(uiPassword))
        {
            app.Configuration["OccasusUI:Password"] = uiPassword;
        }

        app.MapFallbackToPage("/occasus/{*path:nonfile}", "/_OccasusHost");

        app.UseRouting();

        app.UseStaticFiles();

        app.MapBlazorHub("/occasus/_blazor");

    }

}
