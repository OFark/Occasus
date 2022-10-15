using Humanizer;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using MudBlazor;
using Occasus.Settings.Interfaces;
using Occasus.Settings.Models;
using System.Text.Json;

namespace Occasus.Pages
{
    public partial class SettingTab
    {
        private IEnumerable<SettingProperty> editableProperties = default!;
        private EditForm? form = default!;
        private bool formIsValid;
        ValidateOptionsResult? validateOptionsResult;
        public string CardTitle => Title ?? Setting.Type.Name.Humanize();
        [Inject] public IJSRuntime JS { get; set; } = default!;
        [Inject] public ISnackbar Snackbar { get; set; } = default!;
        [Parameter]
        public Action<object?>? OnSave { get; set; }

        [Parameter, EditorRequired]
        public SettingBox Setting { get; set; } = default!;

        [Inject] public ISettingService SettingService { get; set; } = default!;
        [Parameter]
        public string? Title { get; set; }

        [Parameter]
        public CancellationToken Token { get; set; } = new CancellationTokenSource().Token;
        private string? formId => $"form_{Setting.Type.Name}";
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await JS.InvokeVoidAsync("PreventEnterKey", formId).ConfigureAwait(false);
        }

        protected override async Task OnInitializedAsync()
        {

            editableProperties = Setting.EditableProperties;

            await base.OnInitializedAsync();
        }
        private async Task OnInvalidSubmit(EditContext context)
        {
            await InvokeAsync(StateHasChanged);
        }

        private async Task OnValidSubmit(EditContext context)
        {
            var validation = await SettingService.PersistSettingToStorage(Setting, Token);

            validateOptionsResult = validation;

            await InvokeAsync(StateHasChanged);
        }
        private void Save(object? x)
        {
            OnSave?.Invoke(x);
            StateHasChanged();
        }

        private async Task Clear()
        {
            await Setting.ClearSettingStorageAsync().ConfigureAwait(false);
            await Setting.ReloadSettingsFromStorageAsync().ConfigureAwait(false);
            Setting = SettingService.ReloadFromConfiguration(Setting);
        }

        private async Task CopyToClipboard()
        {
            var json = JsonSerializer.Serialize(Setting.Value);
            var success = await JS.InvokeAsync<bool>("clipboardCopy.copyText", json).ConfigureAwait(false);

            if (success is bool s && s)
            {
                Snackbar.Add($"{CardTitle} copied to clipboard", Severity.Success);
            }
        }

        private async Task PasteFromClipboard()
        {
            var jsonRequest = await JS.InvokeAsync<string>("clipboardCopy.pasteText").ConfigureAwait(false);

            if (jsonRequest is string json)
            {

                try
                {
                    var val = JsonSerializer.Deserialize(json, Setting.Type);

                    if (val is null)
                    {
                        Snackbar.Add($"Clipboard contents could not be deserialized into {CardTitle}", Severity.Warning);
                        return;
                    }

                    Setting.SetValue(val);
                }
                catch (Exception ex)
                {
                    Snackbar.Add($"Error Parsing Clipboard: {ex.Message}", Severity.Error);
                }
            }
        }

    }
}
