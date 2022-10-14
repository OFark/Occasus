using Microsoft.Extensions.FileProviders;
using System.Reflection;

namespace Occasus.UI;

public static class WebApplicationExtensions
{

    private static Assembly ThisAssembly => Assembly.GetAssembly(typeof(WebApplicationExtensions))!;
    
    private static ILogger? logger = null;
    private static ILogger CreateLogger(IServiceProvider services) => services.GetRequiredService<ILogger<Program>>();

    
    public static void UseOccasusUI(this WebApplication app, string? uiPassword = null)
    {
        logger ??= CreateLogger(app.Services);

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
