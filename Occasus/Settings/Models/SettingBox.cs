using Humanizer;
using Occasus.Attributes;
using Occasus.Converters;
using Occasus.Helpers;
using Occasus.Repository.Interfaces;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Occasus.Settings.Models
{
    public sealed class SettingBox
    {
        private readonly JsonSerializerOptions jsonSerializerOptions;

        private readonly IOptionsStorageRepository? Repository;
        private object _value;
        private int startingHash;
        private int? restartHash;

        public SettingBox(Type type, IOptionsStorageRepository? repository, object? value = null)
        {
            Type = type;
            this.Repository = repository;
            _value = value ?? (type.IsArray ? Array.CreateInstance(type.GetElementType()!, 0) : Activator.CreateInstance(type)!);

            HumanTitle = GetTitle(type);

            jsonSerializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.General);
            jsonSerializerOptions.Converters.Add(new DateOnlyConverter());
            jsonSerializerOptions.Converters.Add(new TimeOnlyConverter());


        }

        internal string HumanTitle { get; }

        internal IEnumerable<SettingProperty> EditableProperties => Type.GetOptionableProperties().Select(x => new SettingProperty(x));
        internal bool HasChanged => JsonSerializer.Serialize(Value, jsonSerializerOptions).GetHashCode() != startingHash;
        internal bool HasRepository => Repository is not null;
        internal bool IsDefault => Value == Activator.CreateInstance(Type);
        internal bool RequiresRestart { get; private set; }
        internal Type Type { get; set; }
        internal object Value
        {
            get => _value; private set
            {
                _value = value ?? Activator.CreateInstance(Type)!;
                SetHash();
                restartHash ??= GetRestartRequiredHash();
            }
        }
        internal async Task ClearSettingStorageAsync(CancellationToken cancellation = default)
        {
            if (Repository is null)
            {
                throw new InvalidOperationException("Setting has no repository");
            }

            await Repository.ClearSettings(Type.Name, cancellation).ConfigureAwait(false);
        }

        internal object LoadValueFromConfiguration(IConfiguration config)
        {
            var section = config.GetSection(Type.Name);
            Value = section.Get(Type);

            return Value;
        }

        internal async Task PersistSettingToStorageAsync(ILogger? logger, CancellationToken cancellation = default)
        {
            if (Repository is null)
            {
                throw new InvalidOperationException("Setting has no repository");
            }

            await Repository.StoreSetting(Value, Type, cancellation).ConfigureAwait(false);

            SetHash();
            RequiresRestart = restartHash != GetRestartRequiredHash();
        }

        internal async Task ReloadSettingsFromStorageAsync(CancellationToken cancellation = default)
        {
            if (Repository is null)
            {
                throw new InvalidOperationException("Setting has no repository");
            }

            await Repository.ReloadSettings(cancellation).ConfigureAwait(false);
        }

        internal void SetValue(object newValue)
        {
            if (newValue.GetType() == Type)
            {
                _value = newValue;
                return;
            }

            throw new InvalidOperationException("Cannot set value Types do not match");
        }
        private void SetHash()
        {
            startingHash = JsonSerializer.Serialize(Value, jsonSerializerOptions).GetHashCode();
        }


        private int GetRestartRequiredHash()
        {
            var restartRequiredAttribute = Attribute.GetCustomAttribute(Type, typeof(RestartRequiredAttribute)) is not null;

            if(restartRequiredAttribute)
            {
                return JsonSerializer.Serialize(Value, jsonSerializerOptions).GetHashCode();
            }

            var value = JsonSerializer.Deserialize(JsonSerializer.Serialize(Value), Type);

            value = NullifyNotRestartRequiredProperties(value!);

            return JsonSerializer.Serialize(value, new JsonSerializerOptions() { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault | JsonIgnoreCondition.WhenWritingNull }).GetHashCode();
        }
        

        private static object NullifyNotRestartRequiredProperties(object value)
        {
            var properties = value.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var prop in properties)
            {
                var restartRequired = Attribute.GetCustomAttribute(prop, typeof(RestartRequiredAttribute)) is not null;

                if (restartRequired)
                {
                    continue;
                }

                var propValue = prop.GetValue(value);

                if (propValue is not null && 
                    !prop.PropertyType.IsSimple() && 
                    !prop.PropertyType.IsCollection() &&
                    !prop.PropertyType.IsDictionary() &&
                    prop.PropertyType.GetProperties(BindingFlags.Public | BindingFlags.Instance).Any())
                {
                    prop.SetValue(value, NullifyNotRestartRequiredProperties(propValue));
                    continue;
                }

                prop.SetValue(value, default);
            }

            return value;
        }


        private string GetTitle(Type type) => (Attribute.GetCustomAttribute(type, typeof(DisplayAttribute)) as DisplayAttribute)?.Name ?? type.Name.Humanize(LetterCasing.Title);

    }
}
