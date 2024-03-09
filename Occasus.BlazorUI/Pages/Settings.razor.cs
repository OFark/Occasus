using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using MudBlazor;
using Occasus.Settings;
using Occasus.Settings.Interfaces;
using Occasus.Settings.Models;

namespace Occasus.BlazorUI.Pages
{
    public partial class Settings : IDisposable
    {
        [Inject] IHostApplicationLifetime AppLifetime { get; set; } = default!;
        [Inject] public ISettingService SettingService { get; set; } = default!;
        [Inject] public ISnackbar Snackbar { get; set; } = default!;
        [Inject] public IDialogService DialogService { get; set; } = default!;
        [Inject] public IConfiguration Configuration { get; set; } = default!;
        [Inject] OccasusMessageStore MessageStore { get; set; } = default!;
        [Inject] ProtectedSessionStorage ProtectedSessionStore { get; set; } = default!;
        [Parameter] public EventCallback<object> OnChange { get; set; }

        private IEnumerable<SettingBox> settings = default!;

        private readonly CancellationTokenSource cts = new();

        private string? UiPassword => Configuration["OccasusUI:Password"];
        private string? password;

        private bool Authenticated => UiPassword == password;


        protected override async Task OnInitializedAsync()
        {
            if (string.IsNullOrWhiteSpace(UiPassword) || UiPassword == password)
            {
                settings = SettingService.GetSettings();
            }

            await base.OnInitializedAsync();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender && !string.IsNullOrWhiteSpace(UiPassword))
            {
                await InvokeAsync(async () => password = (await ProtectedSessionStore.GetAsync<string>(nameof(password)).ConfigureAwait(false)).Value);
            }

            if (!string.IsNullOrWhiteSpace(UiPassword) && UiPassword != password)
            {
                var dialog = DialogService.Show<PasswordDialog>("Password");
                var result = await dialog.Result;

                if (!result.Canceled)
                {

                    if (result.Data?.ToString() is string pw && !string.IsNullOrWhiteSpace(pw))
                    {
                        password = pw;
                        await InvokeAsync(async () => await ProtectedSessionStore.SetAsync("password", password).ConfigureAwait(false));
                    }
                }

                if (string.IsNullOrWhiteSpace(UiPassword) || UiPassword == password)
                {
                    settings = SettingService.GetSettings();
                }
                StateHasChanged();
            }

            if (firstRender)
            {
                await DoSettingReload().ConfigureAwait(false);
            }

            await base.OnAfterRenderAsync(firstRender);
        }

        private async Task ClearSettings()
        {
            var confirm = await DialogService.ShowMessageBox("Clear All Settings", "Are you sure you want to delete all settings?", "Yes", "No", null, new DialogOptions() { CloseButton = true, CloseOnEscapeKey = true, DisableBackdropClick = true });
            if (confirm ?? false)
            {
                await SettingService.ClearAllSettings(cts.Token).ConfigureAwait(false);

                settings = SettingService.GetSettings();
                Snackbar.Add("Settings have been cleared", Severity.Warning);

                await InvokeAsync(StateHasChanged);
            }
        }

        private async Task ReloadSettings()
        {
            bool result = await DialogService.ShowMessageBox(
            "Warning",
            "This will refresh all settings from storage",
            yesText: "Reload", cancelText: "Cancel") ?? false;

            if (result)
            {
                await DoSettingReload();

                Snackbar.Add("Settings have been reloaded", Severity.Info);

                await InvokeAsync(StateHasChanged);
            }
        }

        private async Task DoSettingReload()
        {

            await SettingService.ReloadAllSettings(cts.Token).ConfigureAwait(false);
            settings = SettingService.GetSettings();

            await InvokeAsync(StateHasChanged);
        }

        private const string restartRequiredMessage = "Settings have changed that require an application restart";

        private async Task SomethingHasChanged(object _)
        {
            MessageStore.Add(restartRequiredMessage, settings.Any(s => s.RequiresRestart));

            await InvokeAsync(StateHasChanged).ConfigureAwait(false);
        }

        public async Task Logout()
        {
            await ProtectedSessionStore.DeleteAsync(nameof(password));
            password = null;
        }

        public async Task FullRestart()
        {
            bool? result = await DialogService.ShowMessageBox(
            "Warning",
            "This will shutdown the entire application! Make sure the hosting provider will restart it automatically",
            yesText: "Restart!", cancelText: "Cancel");

            if (result ?? false)
            {
                AppLifetime.StopApplication();
            }
        }

        public void Dispose()
        {
            if (cts.Token.CanBeCanceled)
            {
                cts.Cancel();
            }

            GC.SuppressFinalize(this);
        }
    }
}
