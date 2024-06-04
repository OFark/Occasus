using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using MudBlazor;
using Occasus.Settings.Interfaces;
using Occasus.Settings.Models;
using System.Text.Json;

namespace Occasus.BlazorUI.Pages
{
    public partial class SettingTab
    {
        public EditContext editContext = default!;
        private bool doValidate;
        private IEnumerable<SettingProperty> editableProperties = default!;
        public string CardTitle => Setting.HumanTitle;
        [Inject] public IJSRuntime JS { get; set; } = default!;
        [Parameter] public EventCallback<object> OnSave { get; set; }
        [Parameter] public EventCallback<bool> OnValidate { get; set; }

        [Parameter, EditorRequired]
        public SettingBox Setting { get; set; } = default!;

        [Inject] public ISettingService SettingService { get; set; } = default!;
        [Inject] public ISnackbar Snackbar { get; set; } = default!;

        [Parameter]
        public CancellationToken Token { get; set; } = new CancellationTokenSource().Token;

        private string? FormId => $"form_{Setting.Type.Name}";

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await JS.InvokeVoidAsync("PreventEnterKey", FormId).ConfigureAwait(true);

            if (doValidate || firstRender)
            {
                await Validate().ConfigureAwait(false);
            }
        }

        protected override async Task OnInitializedAsync()
        {
            editableProperties = Setting.EditableProperties;

            editContext = new EditContext(Setting.Value);
            editContext.OnValidationStateChanged += (o, v) =>
            {
                if (Setting.IsValid && editContext.GetValidationMessages().Any())
                {
                    Setting.ValidationResult = ValidateOptionsResult.Fail(editContext.GetValidationMessages());
                }
            };

            await base.OnInitializedAsync();
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

        private async Task OnInvalidSubmit(EditContext context)
        {
            await InvokeAsync(StateHasChanged);
        }

        private Task OnTabClick(MouseEventArgs args)
        {
            doValidate = true;
            return Task.CompletedTask;
        }

        private async Task OnValidSubmit(EditContext context)
        {
            await SettingService.PersistSettingToStorage(Setting, Token);

            await OnSave.InvokeAsync();
            await InvokeAsync(StateHasChanged);
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

        private async Task Save(object? x)
        {
            await OnSave.InvokeAsync(x);
            await InvokeAsync(StateHasChanged);
        }

        private async Task Validate()
        {
            editContext.Validate();
            editContext.NotifyValidationStateChanged();

            StateHasChanged();
            await OnValidate.InvokeAsync(Setting.IsValid);
            doValidate = false;
        }
    }
}