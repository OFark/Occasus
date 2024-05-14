using Humanizer;
using Microsoft.Extensions.Configuration;
using Occasus.Attributes;
using Occasus.Converters;
using Occasus.Helpers;
using Occasus.Repository.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Occasus.Settings.Models;

public sealed class SettingBox
{
    private readonly JsonSerializerOptions jsonSerializerOptions;

    private readonly IOptionsStorageRepository? Repository;
    private readonly JsonSerializerOptions restartRequiredJsonOptions = new() { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault | JsonIgnoreCondition.WhenWritingNull };
    private object _value;
    private int? restartHash;
    private int startingHash;

    public SettingBox(Type type, IOptionsStorageRepository? repository, object? value = null)
    {
        Type = type;
        Repository = repository;
        _value = value ?? (type.IsArray ? Array.CreateInstance(type.GetElementType()!, 0) : Activator.CreateInstance(type)!);

        HumanTitle = GetTitle(type);

        jsonSerializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.General);
        jsonSerializerOptions.Converters.Add(new DateOnlyConverter());
        jsonSerializerOptions.Converters.Add(new TimeOnlyConverter());
    }

    public IEnumerable<SettingProperty> EditableProperties => Type.GetOptionableProperties().Select(x => new SettingProperty(x));
    public bool HasChanged => JsonSerializer.Serialize(Value, jsonSerializerOptions).GetHashCode() != startingHash;
    public string HumanTitle { get; }
    public bool IsDefault => Value == Activator.CreateInstance(Type);
    public bool RequiresRestart { get; private set; }
    public bool IsValid { get; internal set; } = true;
    public Type Type { get; set; }

    [NotNull]
    public object? Value
    {
        get => _value; private set
        {
            _value = value ?? Activator.CreateInstance(Type)!;
            SetHash();
            restartHash ??= GetRestartRequiredHash();
        }
    }

    internal bool HasRepository => Repository is not null;

    public async Task ClearSettingStorageAsync(CancellationToken cancellation = default)
    {
        if (Repository is null)
        {
            throw new InvalidOperationException("Setting has no repository");
        }

        await Repository.ClearSettings(Type.Name, cancellation).ConfigureAwait(false);
    }

    public async Task ReloadSettingsFromStorageAsync(CancellationToken cancellation = default)
    {
        if (Repository is null)
        {
            throw new InvalidOperationException("Setting has no repository");
        }

        await Repository.ReloadSettings(cancellation).ConfigureAwait(false);
    }

    public void SetValue(object newValue)
    {
        if (newValue.GetType() == Type)
        {
            _value = newValue;
            return;
        }

        throw new InvalidOperationException("Cannot set value Types do not match");
    }

    internal object LoadValueFromConfiguration(IConfiguration config)
    {
        var section = config.GetSection(Type.Name);
        Value = section.Get(Type);

        return Value;
    }

    internal async Task PersistSettingToStorageAsync(CancellationToken cancellation = default)
    {
        if (Repository is null)
        {
            throw new InvalidOperationException("Setting has no repository");
        }

        await Repository.StoreSetting(Value, Type, cancellation).ConfigureAwait(false);

        SetHash();
        RequiresRestart = restartHash != GetRestartRequiredHash();
    }

    private static string GetTitle(Type type) => (Attribute.GetCustomAttribute(type, typeof(DisplayAttribute)) as DisplayAttribute)?.Name ?? type.Name.Humanize(LetterCasing.Title);

    private static object NullifyNotRestartRequiredProperties(object value)
    {
        var properties = value.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var prop in properties)
        {
            var restartRequired = Attribute.GetCustomAttribute(prop, typeof(RestartRequiredAttribute)) is not null;

            if (restartRequired || !prop.CanWrite)
            {
                continue;
            }

            var propValue = prop.GetValue(value);

            if (propValue is not null &&
                !prop.PropertyType.IsSimple() &&
                !prop.PropertyType.IsCollection() &&
                !prop.PropertyType.IsDictionary() &&
                prop.PropertyType.GetProperties(BindingFlags.Public | BindingFlags.Instance).Length != 0)
            {
                prop.SetValue(value, NullifyNotRestartRequiredProperties(propValue));
                continue;
            }

            prop.SetValue(value, default);
        }

        return value;
    }

    private int GetRestartRequiredHash()
    {
        var restartRequiredAttribute = Attribute.GetCustomAttribute(Type, typeof(RestartRequiredAttribute)) is not null;

        if (restartRequiredAttribute)
        {
            return JsonSerializer.Serialize(Value, jsonSerializerOptions).GetHashCode();
        }

        var value = JsonSerializer.Deserialize(JsonSerializer.Serialize(Value), Type);

        value = NullifyNotRestartRequiredProperties(value!);

        return JsonSerializer.Serialize(value, restartRequiredJsonOptions).GetHashCode();
    }

    private void SetHash()
    {
        startingHash = JsonSerializer.Serialize(Value, jsonSerializerOptions).GetHashCode();
    }
}