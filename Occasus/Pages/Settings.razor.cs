using Microsoft.AspNetCore.Components;
using MudBlazor;
using Occasus.Settings.Interfaces;
using Occasus.Settings.Models;

namespace Occasus.Pages
{
    public partial class Settings : IDisposable
    {
        [Inject]
        public ISettingService SettingService { get; set; } = default!;
        [Inject]
        public ISnackbar Snackbar { get; set; } = default!;
        [Inject]
        public IDialogService DialogService { get; set; } = default!;
        [Inject]
        public IConfiguration configuration { get; set; } = default!;

        private IEnumerable<SettingBox> settings = default!;

        private readonly CancellationTokenSource cts = new();

        private string? uiPassword => configuration["OccasusUI:Password"];
        private string? password;



        protected override async Task OnInitializedAsync()
        {
            if (string.IsNullOrWhiteSpace(uiPassword) || uiPassword == password)
            {
                settings = SettingService.GetSettings();
            }

            await base.OnInitializedAsync();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!string.IsNullOrWhiteSpace(uiPassword) && uiPassword != password)
            {
                var dialog = DialogService.Show<PasswordDialog>("Password");
                var result = await dialog.Result;

                if (!result.Cancelled)
                {
                    password = result.Data.ToString();
                }

                if (string.IsNullOrWhiteSpace(uiPassword) || uiPassword == password)
                {
                    settings = SettingService.GetSettings();
                }
                StateHasChanged();
            }

            if(firstRender)
            {
                await ReloadSettings().ConfigureAwait(false);
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
            await SettingService.ReloadAllSettings(cts.Token).ConfigureAwait(false);
            settings = SettingService.GetSettings();

            Snackbar.Add("Settings have been reloaded", Severity.Info);

            await InvokeAsync(StateHasChanged);
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
