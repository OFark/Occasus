using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;
using Occasus.Helpers;
using Occasus.Settings.Models;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Reflection.Metadata;

namespace Occasus.BlazorUI.Pages;

public partial class SettingEditorGeneric<T>
{
    private readonly Variant inputVariant = Variant.Text;

    private string? AdornmentIcon;
    private bool displayPassword = false;
    private Type nonNullableType = default!; //This is created by OnInit based on Type

    [Parameter]
    public bool AutoFocus { get; set; }

    [Parameter]
    public string? Class { get; set; }

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public string? Label { get; set; }

    [Parameter]
    public EventCallback<KeyboardEventArgs> OnKeyPressed { get; set; }

    [Parameter]
    public object? POCO { get; set; }

    [Parameter]
    public bool Required { get; set; }

    [Parameter]
    public string? RequiredMessage { get; set; }

    [Parameter]
    public SettingProperty? SettingProperty { get; set; }

    [Parameter]
    public InputType TypeOfInput { get; set; }

    [Parameter]
    public ValidationAttribute? Validation { get; set; }

    [Parameter]
    public T? Value { get; set; } = default!;

    [Parameter]
    public EventCallback<object?> ValueAdded { get; set; }

    [Parameter]
    public EventCallback<object?> ValueChanged { get; set; }

    [Parameter]
    public EventCallback<object?> ValueDeleted { get; set; }

    private InputType InputType => displayPassword ? InputType.Text
                                                   : nonNullableType == typeof(TimeSpan) || nonNullableType == typeof(TimeOnly) ? InputType.Time
                                                   : nonNullableType == typeof(DateOnly) ? InputType.Date
                                                   : TypeOfInput;

    protected override void OnInitialized()
    {
        AdornmentIcon = TypeOfInput == InputType.Password ? Icons.Material.Rounded.Password : null;

        nonNullableType = typeof(T).NonNullableType();

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

    private RenderFragment GetComponent() => builder =>
    {
        var componentType = GetComponentType();
        builder.OpenComponent(0, componentType);

        builder.AddAttribute(1, nameof(MudBaseInput<T>.Value), Value);
        builder.AddAttribute(2, nameof(MudBaseInput<T>.ValueChanged), EventCallback.Factory.Create<T>(this, value => { SetValue(value); }));

        builder.AddAttribute(3, nameof(MudFormComponent<T, string>.Validation), Validation);
        builder.AddAttribute(4, nameof(MudFormComponent<T, string>.Class), Class);
        builder.AddAttribute(5, nameof(MudBaseInput<T>.Variant), inputVariant);
        builder.AddAttribute(6, nameof(MudBaseInput<T>.Label), Label);
        builder.AddAttribute(7, nameof(MudBaseInput<T>.Disabled), Disabled);
        builder.AddAttribute(8, nameof(MudFormComponent<T, string>.Required), Required);
        builder.AddAttribute(9, nameof(MudFormComponent<T, string>.RequiredError), RequiredMessage);
        builder.AddAttribute(9, nameof(MudFormComponent<T, string>.For), GetPropertyExpression());

        if (componentType != typeof(MudCheckBox<T>))
        {
            builder.AddAttribute(10, nameof(MudBaseInput<T>.AutoFocus), AutoFocus);
            builder.AddAttribute(11, nameof(MudBaseInput<T>.Immediate), nonNullableType != typeof(Guid) && !nonNullableType.IsEnum);
            builder.AddAttribute(12, nameof(MudBaseInput<T>.Adornment), Adornment.End);
            builder.AddAttribute(13, nameof(MudBaseInput<T>.AdornmentIcon), AdornmentIcon);
            builder.AddAttribute(14, nameof(MudBaseInput<T>.OnAdornmentClick), EventCallback.Factory.Create<MouseEventArgs>(this, AdornmentClick));
            builder.AddAttribute(15, nameof(MudBaseInput<T>.OnKeyUp), EventCallback.Factory.Create(this, async (KeyboardEventArgs args) => { await KeyPressed(args); }));
        }

        if (nonNullableType == typeof(DateTime))
        {
            builder.AddAttribute(16, nameof(MudBaseInput<T>.TextUpdateSuppression), false);
        }

        if (componentType == typeof(MudTextField<T>))
        {
            builder.AddAttribute(17, nameof(MudTextField<T>.InputType), InputType);
        }

        if (nonNullableType == typeof(string))
        {
            builder.AddAttribute(19, "autocomplete", "new-password");
        }

        if (componentType == typeof(MudCheckBox<T>))
        {
            builder.AddAttribute(20, nameof(MudCheckBox<T>.TriState), typeof(T).IsNullable());
            builder.AddAttribute(21, nameof(MudCheckBox<T>.Color), Color.Success);
        }

        if (componentType == typeof(MudSelect<T>))
        {
            builder.AddAttribute(22, nameof(MudSelect<T>.ChildContent), GetEnumValueItems());
        }

        AddConverter(23, builder);

        builder.CloseComponent();
    };

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "ASP0006:Do not use non-literal sequence numbers", Justification = "Pass through method")]
    private void AddConverter(int sequence, RenderTreeBuilder builder)
    {
        if (nonNullableType == typeof(DateTime))
        {
            builder.AddAttribute(sequence, nameof(MudBaseInput<T>.Converter),
            new Converter<T>()
            {
                SetFunc = value => value is null ? null : (value as DateTime?)?.ToString(InputType == InputType.Date ? "yyyy-MM-dd" : "s"),
                GetFunc = text => DateTime.TryParse(text, out var dt) && dt is T t ? t : default
            });
        }

        if (nonNullableType == typeof(DateOnly))
        {
            builder.AddAttribute(sequence, nameof(MudBaseInput<T>.Converter), 
            new Converter<T>()
            {
                SetFunc = value => value is null ? null : (value as DateOnly?)?.ToString("yyyy-MM-dd"),
                GetFunc = text => DateOnly.TryParse(text, out var ts) && ts is T t ? t : default
            });
        }

        if (nonNullableType == typeof(TimeOnly))
        {
            builder.AddAttribute(sequence, nameof(MudBaseInput<T>.Converter), 
                new Converter<T>()
            {
                SetFunc = value => value is null ? null : (value as TimeOnly?)?.ToString(),
                GetFunc = text => TimeOnly.TryParse(text, out var ts) && ts is T t ? t : default
            });
        }
    }

    private Type GetComponentType()
    {
        return nonNullableType switch
        {
            Type t when t == typeof(decimal) || t == typeof(double) || t == typeof(float) || t == typeof(short) || t == typeof(int) || t == typeof(long) => typeof(MudNumericField<T>),
            Type t when t == typeof(bool) => typeof(MudCheckBox<T>),
            Type t when t.IsEnum => typeof(MudSelect<T>),
            _ => typeof(MudTextField<T>)
        };
    }

    private RenderFragment GetEnumValueItems() => builder =>
            {
                if (typeof(T).IsNullable())
                {
                    builder.OpenComponent<MudSelectItem<T>>(23);
                    builder.AddAttribute(24, nameof(MudSelectItem<T>.Value), (object?)null);
                    builder.AddAttribute(25, nameof(MudSelectItem<T>.ChildContent), new RenderFragment(b => b.AddContent(0, "Select an option:")));
                    builder.CloseComponent();
                }
                foreach (var item in Enum.GetValues(nonNullableType))
                {
                    builder.OpenComponent<MudSelectItem<T>>(0);
                    builder.AddAttribute(3, nameof(MudSelectItem<T>.Value), item);
                    builder.AddAttribute(4, nameof(MudSelectItem<T>.ChildContent), new RenderFragment(b => b.AddContent(0, item)));
                    builder.CloseComponent();
                }
            };

    private Expression<Func<T?>> GetPropertyExpression()
    {
        if (SettingProperty is null || SettingProperty.PropertyInfo.ReflectedType is null) return null;

        var parameter = Expression.Constant(POCO);

        var property = Expression.Property(parameter, SettingProperty.PropertyInfo);
        var lambda = Expression.Lambda<Func<T?>>(property);
        return lambda;
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

    private void SetValue(T? value)
    {
        if (nonNullableType.IsEnum && value is not null && Enum.ToObject(nonNullableType, value) is T eVal)
        {
            value = eVal;
        }

        Value = typeof(T).IsNullable() ? value : value ?? default;
        ValueChanged.InvokeAsync(Value);
    }
}