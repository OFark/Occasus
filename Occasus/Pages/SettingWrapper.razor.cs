using Humanizer;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Occasus.Helpers;
using Occasus.Settings;
using Occasus.Settings.Interfaces;
using Occasus.Settings.Models;
using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

namespace Occasus.Pages;

public partial class SettingWrapper
{
    private bool _addNewValue;
    private int _index = 0;
    private bool _useNewValue;
    private object? _value;

    private MudTabs? DictionaryTabs;
    private InputType InputType;

    private InputType KeyInputType;

    private Type? KeyType;

    private string? Label;
    private string? NewKey;

    private bool Required;

    private string? RequiredMessage;

    private ValidationAttribute? validationAttributes;

    private Type? ValueType;

    [Parameter]
    public EventCallback<object> OnSave { get; set; }

    [Parameter]
    public object? POCO { get; set; }

    [Parameter, EditorRequired]
    public SettingProperty SettingProperty { get; set; } = default!;

    //This is not the POCO
    public object? Value
    {
        get => _value;
        set
        {
            _value = value;
            SetValue(value);
            StateHasChanged();
        }
    }

    [Inject]
    internal IPOCOService POCOService { get; set; } = default!;
    protected override void OnAfterRender(bool firstRender)
    {
        if (_useNewValue == true && Value is IDictionary && DictionaryTabs is not null)
        {
            DictionaryTabs.ActivatePanel(DictionaryTabs.Panels[DictionaryTabs.Panels.Count - 1]);
            StateHasChanged();
            _useNewValue = false;
        }
    }

    protected override void OnInitialized()
    {
        if (SettingProperty is not null && POCO is not null)
        {
            var display = Attribute.GetCustomAttribute(SettingProperty.PropertyInfo, typeof(DisplayAttribute), false) as DisplayAttribute;
            var required = Attribute.GetCustomAttribute(SettingProperty.PropertyInfo, typeof(RequiredAttribute), false) as RequiredAttribute;
            validationAttributes = new CompositeValidationAttribute(Attribute.GetCustomAttributes(SettingProperty.PropertyInfo, typeof(ValidationAttribute), false).Cast<ValidationAttribute>());


            Required = required is not null;
            RequiredMessage = required?.ErrorMessage;
            Label = (display?.Name ?? SettingProperty.PropertyInfo.Name).Humanize();
            NewKey = $"New {Label.Humanize().Singularize()}";
            ValueType = SettingProperty.ValueType;
            InputType = SettingProperty.InputAttribute.InputType;

            KeyType = SettingProperty.NotNullType.DictionaryKeyType();
            KeyInputType = KeyType?.NonNullableType() == typeof(DateTime) ? InputType.DateTimeLocal : InputType.Text;



            if (SettingProperty.NotNullType.IsEnumerable())
            {
                var length = SettingProperty.PropertyInfo.GetIndexParameters().Length;
                _value = SettingProperty.PropertyInfo.GetValue(POCO, Enumerable.Range(0, length).Cast<object>().ToArray());
            }
            else
            {
                _value = POCO is null ? null : SettingProperty?.PropertyInfo.GetValue(POCO);
            }


        }
        base.OnInitialized();
    }
    private void AddNewValue()
    {
        _addNewValue = true;
    }

    private void AddValue()
    {
        if (SettingProperty.NewValue is not null || SettingProperty.NotNullType.IsCollection())
        {
            POCOService.AddValue(POCO, SettingProperty).Match(some => Value = some, () => { });
            _useNewValue = true;
            _addNewValue = false;
        }
    }
    private void ChangeDictionaryItem(object? value, object key)
    {

        if (SettingProperty is not null)
        {
            POCOService.ChangeDictionaryItem(POCO, SettingProperty.PropertyInfo, value, key).Match(some => Value = some, () => { });
        }
    }

    private void ChangeListItem(object? value, int index)
    {
        if (SettingProperty is not null)
        {
            POCOService.ChangeListItem(POCO, SettingProperty.PropertyInfo, value, index).Match(some => Value = some, () => { });
        }
    }

    private void RemoveValue(int index)
    {
        if (SettingProperty is not null)
        {
            POCOService.RemoveValue(POCO, SettingProperty, index).Match(some => Value = some, () => { });

            if (DictionaryTabs is not null)
            {
                DictionaryTabs.ActivatePanel(0);
                StateHasChanged();
            }
        }
    }

    private void RemoveValue(object key)
    {
        if (SettingProperty is not null)
        {
            POCOService.RemoveValueWithKey(POCO, SettingProperty.PropertyInfo, key).Match(some => Value = some, () => { });

            if (DictionaryTabs is not null)
            {
                DictionaryTabs.ActivatePanel(0);
                StateHasChanged();
            }
        }
    }

    private void SetValue(object? value)
    {
        Debug.Assert(SettingProperty is not null);
        Debug.Assert(POCO is not null);

        if (SettingProperty is not null)
        {
            POCOService.SetValue(POCO, SettingProperty.PropertyInfo, value);

            OnSave.InvokeAsync(POCO);
        }

        StateHasChanged();
    }
}
