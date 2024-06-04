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
        private const string restartRequiredMessage = "Settings have changed that require an application restart";
        private readonly CancellationTokenSource cts = new();
        private string? password;
        private IList<SettingBox> settings = default!;
        private MudTabs tabs = default!;
        [Inject] public IConfiguration Configuration { get; set; } = default!;
        [Inject] public IDialogService DialogService { get; set; } = default!;
        [Parameter] public EventCallback<object> OnChange { get; set; }
        [Inject] public ISettingService SettingService { get; set; } = default!;
        [Inject] public ISnackbar Snackbar { get; set; } = default!;
        [Inject] private IHostApplicationLifetime AppLifetime { get; set; } = default!;
        private bool Authenticated => UiPassword == password;
        [Inject] private OccasusMessageStore MessageStore { get; set; } = default!;
        [Inject] private ProtectedSessionStorage ProtectedSessionStore { get; set; } = default!;
        private string? UiPassword => Configuration["OccasusUI:Password"];

        public void Dispose()
        {
            if (cts.Token.CanBeCanceled)
            {
                cts.Cancel();
            }

            GC.SuppressFinalize(this);
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

        public async Task Logout()
        {
            await ProtectedSessionStore.DeleteAsync(nameof(password));
            password = null;
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
                ActivateFirstInvalidTab();
            }

            await base.OnAfterRenderAsync(firstRender);
        }

        protected override async Task OnInitializedAsync()
        {
            if (string.IsNullOrWhiteSpace(UiPassword) || UiPassword == password)
            {
                settings = SettingService.GetSettings();
            }

            await base.OnInitializedAsync();
        }

        private void ActivateFirstInvalidTab()
        {
            var firstInvalidSetting = settings.FirstOrDefault(s => !s.IsValid);
            if (firstInvalidSetting != null)
            {
                var index = settings.IndexOf(firstInvalidSetting);
                tabs.ActivatePanel(index);
            }
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

        private async Task DoSettingReload()
        {
            await SettingService.ReloadAllSettings(cts.Token).ConfigureAwait(false);
            settings = SettingService.GetSettings();

            await InvokeAsync(StateHasChanged);
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

        private async Task SomethingHasChanged(object _)
        {
            MessageStore.Add(restartRequiredMessage, settings.Any(s => s.RequiresRestart));

            await InvokeAsync(StateHasChanged).ConfigureAwait(false);
        }

        private Task TabHasValidated(bool valid)
        {
            StateHasChanged();
            return Task.CompletedTask;
        }
    }
}