using Occasus.Converters;
using Occasus.Helpers;
using Occasus.Repository.Interfaces;
using System.Collections;
using System.ComponentModel.Design;
using System.Reflection;
using System.Text.Json;
using static MudBlazor.CategoryTypes;

namespace Occasus.Settings.Models
{
    public sealed class SettingBox
    {
        private readonly JsonSerializerOptions jsonSerializerOptions;

        private object _value;

        private readonly IOptionsStorageRepository? Repository;
        private int StartingHash;

        public SettingBox(Type type, IOptionsStorageRepository? repository, object? value = null)
        {
            Type = type;
            this.Repository = repository;
            _value = value ?? (type.IsArray ? Array.CreateInstance(type.GetElementType()!, 0) : Activator.CreateInstance(type)!);

            jsonSerializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.General);
            jsonSerializerOptions.Converters.Add(new DateOnlyConverter());
            jsonSerializerOptions.Converters.Add(new TimeOnlyConverter());
        }
        internal IEnumerable<SettingProperty> EditableProperties => Type.GetOptionableProperties().Select(x => new SettingProperty(x));
        internal bool HasChanged => JsonSerializer.Serialize(Value, jsonSerializerOptions).GetHashCode() != StartingHash;
        internal bool IsDefault => Value == Activator.CreateInstance(Type);
        internal bool HasRepository => Repository is not null;

        internal Type Type { get; set; }
        internal object Value
        {
            get => _value; private set
            {
                _value = value ?? Activator.CreateInstance(Type)!;
                SetHash();
            }
        }
        internal async Task ClearSettingStorageAsync(CancellationToken cancellation = default)
        {
            if (Repository is null) throw new InvalidOperationException("Setting has no repository");

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
            if (Repository is null) throw new InvalidOperationException("Setting has no repository");

            await Repository.StoreSetting(Value, Type, cancellation).ConfigureAwait(false);

            SetHash();
        }

        internal async Task ReloadSettingsFromStorageAsync(CancellationToken cancellation = default)
        {            
            if (Repository is null) throw new InvalidOperationException("Setting has no repository");

            await Repository.ReloadSettings(cancellation).ConfigureAwait(false);
        }

        private void SetHash()
        {
            StartingHash = JsonSerializer.Serialize(Value, jsonSerializerOptions).GetHashCode();
        }

        

    }
}
