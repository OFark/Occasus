﻿using Humanizer;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Options;
using Occasus.Settings.Interfaces;
using Occasus.Settings.Models;

namespace Occasus.BlazorUI.Pages
{
    public partial class SettingCard
    {
        [Inject] public ISettingService SettingService { get; set; } = default!;

        [Parameter, EditorRequired]
        public SettingBox Setting { get; set; } = default!;

        [Parameter]
        public CancellationToken Token { get; set; } = new CancellationTokenSource().Token;

        [Parameter]
        public Action<object?>? OnSave { get; set; }

        [Parameter]
        public Action<object?>? OnDelete { get; set; }

        [Parameter]
        public string? Title { get; set; }

        [Parameter]
        public bool Disabled { get; set; }

        [Parameter]
        public bool HideTitle { get; set; }

        public string CardTitle => Title ?? Setting.Type.Name.Humanize().Singularize();

        private IEnumerable<SettingProperty> editableProperties = default!;

        private string MudTheme => Disabled ? "mud-theme-error" : "mud-theme-secondary";

        private ValidateOptionsResult? validateOptionsResult;

        protected override async Task OnInitializedAsync()
        {

            editableProperties = Setting.EditableProperties;

            await base.OnInitializedAsync();
        }

        private async Task OnValidSubmit(EditContext context)
        {
            var validation = await SettingService.PersistSettingToStorage(Setting, Token);

            validateOptionsResult = validation;

            await InvokeAsync(StateHasChanged);
        }

        private async Task OnInvalidSubmit(EditContext context)
        {
            await InvokeAsync(StateHasChanged);
        }

        private void Delete(object? x)
        {
            OnDelete?.Invoke(x);
            StateHasChanged();
        }

        private void Save(object? x)
        {
            OnSave?.Invoke(x);
            StateHasChanged();
        }

    }
}
