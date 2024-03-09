using Humanizer;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Occasus.Attributes;
using Occasus.Helpers;
using Occasus.Settings;
using Occasus.Settings.Models;
using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Reflection;
using Occasus.Settings.Interfaces;

namespace Occasus.BlazorUI.Pages;

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

    [Parameter]
    public bool Disabled { get; set; }

    private ValidationAttribute? validationAttributes;

    private Type? ValueType;

    [Parameter]
    public EventCallback<object> OnSave { get; set; }

    [Parameter]
    public object? POCO { get; set; }

    [Parameter, EditorRequired]
    public SettingProperty SettingProperty { get; set; } = default!;

    private string mudTheme => Disabled ? "mud-theme-error" : "mud-theme-dark";

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

            validationAttributes = new CompositeValidationAttribute(Attribute.GetCustomAttributes(SettingProperty.PropertyInfo, typeof(ValidationAttribute), false).Cast<ValidationAttribute>());

            Required = IsRequired(SettingProperty.PropertyInfo, out var message);
            RequiredMessage = message;

            Disabled = Disabled || !IsSettable(SettingProperty.PropertyInfo, out _);

            Label = GetLabel();
            NewKey = $"New {GetName().Humanize().Singularize()}";

            ValueType = SettingProperty.ValueType;
            InputType = SettingProperty.InputAttribute.InputType;

            KeyType = SettingProperty.NotNullType.DictionaryKeyType();
            KeyInputType = KeyType?.NonNullableType() == typeof(DateTime) ? InputType.DateTimeLocal : InputType.Text;

            if (SettingProperty.NotNullType.IsEnumerable())
            {
                var length = SettingProperty.PropertyInfo.GetIndexParameters().Length;
                _value = SettingProperty.PropertyInfo.GetValue(POCO, Enumerable.Range(0, length).Cast<object>().ToArray());
            }
            else if(SettingProperty.NotNullType.IsEnum)
            {
                _value = POCO is null ? null : 
                    Enum.TryParse(SettingProperty.NotNullType, SettingProperty.PropertyInfo.GetValue(POCO)?.ToString(), out var eVal) && eVal is not null ?
                        (int)eVal:
                        null;
            }
            else
            {
                _value = POCO is null ? null : SettingProperty?.PropertyInfo.GetValue(POCO);
            }


        }
        base.OnInitialized();
    }

    private string GetName() 
        => (Attribute.GetCustomAttribute(SettingProperty.PropertyInfo, typeof(DisplayAttribute), false) as DisplayAttribute)?.Name ?? SettingProperty.PropertyInfo.Name.Humanize(LetterCasing.Title);

    private string GetLabel()
    {
        var name = GetName();
        var restartRequired = IsRestartRequired(SettingProperty.PropertyInfo) ? " !" : "";
        var disabled = IsSettable(SettingProperty.PropertyInfo, out var message) ? "" : $" - {message}";

        return $"{name}{restartRequired}{disabled}";
    }

    private static bool IsRestartRequired(PropertyInfo propertyInfo)
        => Attribute.GetCustomAttribute(propertyInfo, typeof(RestartRequiredAttribute), false) as RestartRequiredAttribute is not null;

    private static bool IsRequired(PropertyInfo propertyInfo, out string? message)
    {
        var required = Attribute.GetCustomAttribute(propertyInfo, typeof(RequiredAttribute), false) as RequiredAttribute;

        if (required is not null)
        {
            message = required.ErrorMessage;
            return true;
        }

        message = null;
        return false;
    }

    private static bool IsSettable(PropertyInfo propertyInfo, out string? message)
    {
        message = null;

        var setter = propertyInfo.GetSetMethod(true);

        if (setter is null)
        {
            message = "This property doesn't have a setter";
            return false;
        }

        if(setter.IsAssembly)
        {
            message = "This property is internal";
            return false;
        }

        if(setter.IsFamily)
        {
            message = "This property is protected";
            return false;
        }

        if (setter.IsPrivate)
        {
            message = "This property is private";
            return false;
        }

        return true;
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
