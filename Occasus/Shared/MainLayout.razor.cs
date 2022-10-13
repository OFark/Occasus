using MudBlazor;
using MudBlazor.ThemeManager;

namespace Occasus.Shared;

public partial class MainLayout
{
    readonly MudTheme OccasusTheme = new()
    {
        Palette = new Palette()
        {
            Primary = "#10095a",
            Secondary = "#095A39",
            Tertiary = "#39095A",
            TextPrimary = "#333",
            AppbarBackground = "#ffffff",
            AppbarText = "#10095a",
            
        },

        LayoutProperties = new LayoutProperties()
        {
            DrawerWidthLeft = "260px",
            DrawerWidthRight = "300px"
        },
        Typography = new Typography()
        {
            Default = new() { FontSize = "14pt", FontFamily = new string[] { "Ubuntu", "Helvetica", "Arial", "sans-serif" } }

        }
    };

    private ThemeManagerTheme _themeManager = new();
    public bool _themeManagerOpen = false;

    void OpenThemeManager(bool value)
    {
        _themeManagerOpen = value;
    }

    void UpdateTheme(ThemeManagerTheme value)
    {
        _themeManager = value;
        StateHasChanged();
    }

    protected override void OnInitialized()
    {
        StateHasChanged();
        _themeManager.Theme = OccasusTheme;
    }
}
