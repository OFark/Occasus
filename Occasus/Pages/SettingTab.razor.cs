using Humanizer;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using Occasus.Settings.Interfaces;
using Occasus.Settings.Models;

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

    }
}
