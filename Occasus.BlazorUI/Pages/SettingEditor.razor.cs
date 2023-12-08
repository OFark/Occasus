using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;
using Occasus.Helpers;
using System.ComponentModel.DataAnnotations;

namespace Occasus.BlazorUI.Pages;

public partial class SettingEditor
{
    readonly Variant inputVariant = Variant.Text;

    string? AdornmentIcon;
    private bool displayPassword = false;
    private Type nonNullableType = default!; //This is created by OnInit based on Type

    [Parameter]
    public bool AutoFocus { get; set; }

    [Parameter]
    public string? Class { get; set; }

    [Parameter]
    public string? Label { get; set; }

    [Parameter]
    public EventCallback<KeyboardEventArgs> OnKeyPressed { get; set; }

    [Parameter]
    public bool Required { get; set; }

    [Parameter]
    public string? RequiredMessage { get; set; }

    [Parameter, EditorRequired]
    public Type Type { get; set; } = default!;

    [Parameter]
    public InputType TypeOfInput { get; set; }

    [Parameter]
    public ValidationAttribute? Validation { get; set; }

    [Parameter]
    public object? Value { get; set; } = default!;

    [Parameter]
    public EventCallback<object?> ValueAdded { get; set; }

    [Parameter]
    public EventCallback<object?> ValueChanged { get; set; }

    [Parameter]
    public EventCallback<object?> ValueDeleted { get; set; }
    private InputType InputType => displayPassword ? InputType.Text : TypeOfInput;

    private bool? ValueBool
    {
        get => Value as bool?;
        set => SetValue(value);
    }

    private string? ValueDate
    {
        get => (Value as DateTime?)?.ToString("yyyy-MM-dd");
        set
        {
            Value = value is null ? default : DateTime.TryParse(value, out var result) ? result : default;
            ValueChanged.InvokeAsync(Value);
        }
    }

    private string? ValueDateTime
    {
        get => (Value as DateTime?)?.ToString("s");
        set
        {
            Value = value is null ? default : DateTime.TryParse(value, out var result) ? result : default;
            ValueChanged.InvokeAsync(Value);
        }
    }

    private decimal? ValueDecimal
    {
        get => Value as decimal?;
        set => SetValue(value);
    }

    private double? ValueDouble
    {
        get => Value as double?;
        set => SetValue(value);
    }

    private int? ValueEnum
    {
        get => Value is null ? null : (int)Value;
        set => SetValue(value);
    }

    private float? ValueFloat
    {
        get => Value as float?;
        set => SetValue(value);
    }

    private int? ValueInt
    {
        get => Value as int?;
        set => SetValue(value);
    }

    private short? ValueInt16
    {
        get => Value as short?;
        set => SetValue(value);
    }

    private long? ValueInt64
    {
        get => Value as long?;
        set => SetValue(value);
    }

    private Guid? ValueGuid
    {
        get => Value as Guid?;
        set => SetValue(value);
    }

    private string? ValueString
    {
        get => Value?.ToString();
        set => SetValue(value);
    }

    private string? ValueTime
    {
        get => (Value as DateTime?)?.ToString("T");
        set
        {
            Value = value is null ? default : TimeSpan.TryParse(value, out var result) ? DateTime.MinValue.Add(result) : default;
            ValueChanged.InvokeAsync(Value);
        }
    }

    private TimeSpan? ValueTimeSpan
    {
        get => Value as TimeSpan?;
        set => SetValue(value);
    }

    protected override void OnInitialized()
    {

        AdornmentIcon = TypeOfInput == InputType.Password ? Icons.Material.Rounded.Password : null;

        nonNullableType = Type.NonNullableType();

        base.OnInitialized();
    }

    private async Task AddValueClick()
    {
        await ValueAdded.InvokeAsync(Value);
    }

    private void AdornmentClick()
    {
        displayPassword = !displayPassword;
    }

    private async Task DeleteValueClick()
    {
        await ValueDeleted.InvokeAsync(Value);
    }

    private async Task KeyPressed(KeyboardEventArgs e)
    {
        if (ValueAdded.HasDelegate && e.Key == "Enter")
        {
            await AddValueClick().ConfigureAwait(false);
            Value = default;
            StateHasChanged();
        }

        await OnKeyPressed.InvokeAsync(e).ConfigureAwait(false);
    }
    private void SetValue(object? value)
    {
        if(nonNullableType.IsEnum && value is not null && Enum.ToObject(nonNullableType, value) is object eVal)
        {
            value = eVal;
        }

        Value = Type.IsNullable() ? value : value ?? default;
        ValueChanged.InvokeAsync(Value);
    }

    private Dictionary<string, int> GetEnumValues()
    {
        var vals = new Dictionary<string, int>();
        foreach (var eVal in Enum.GetValues(nonNullableType))
        {
            if (Enum.GetName(nonNullableType, eVal) is string eValName)
            {
                vals.Add(eValName, (int)eVal);
            }
        }
        return vals;
    }
}
