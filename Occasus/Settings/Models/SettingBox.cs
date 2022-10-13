using Occasus.Converters;
using Occasus.Helpers;
using Occasus.Repository.Interfaces;
using System.Collections;
using System.Reflection;
using System.Text.Json;

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
        internal async Task ClearSettingStorageAsync(CancellationToken? cancellation = default)
        {
            if (Repository is null) throw new InvalidOperationException("Setting has no respoitory");

            await Repository.ClearSettings(Type.Name, cancellation).ConfigureAwait(false);
        }

        internal object LoadValueFromConfiguration(IConfiguration config)
        {
            var section = config.GetSection(Type.Name);
            Value = section.Get(Type);

            return Value;
        }

        internal async Task PersistSettingToStorageAsync(ILogger? logger, CancellationToken? cancellation = default)
        {
            if (Repository is null) throw new InvalidOperationException("Setting has no respoitory");

            var settingStorage = Value is not null ? ToSettingItems(Value, new() { Type.Name }, logger) : new() { new(Type.Name, null) };

            foreach (var item in settingStorage)
            {
                if (item.Value is not null)
                {
                    await Repository.StoreSetting(item.Name, item.Value, cancellation).ConfigureAwait(false);
                }
            }

            SetHash();

        }

        internal async Task ReloadSettingsFromStorageAsync(CancellationToken? cancellation = default)
        {            
            if (Repository is null) throw new InvalidOperationException("Setting has no respoitory");

            await Repository.ReloadSettings(cancellation).ConfigureAwait(false);
        }

        private void SetHash()
        {
            StartingHash = JsonSerializer.Serialize(Value, jsonSerializerOptions).GetHashCode();
        }

        private List<SettingStorage> ToSettingItems(object obj, List<string> path, ILogger? logger)
        {

            var results = new List<SettingStorage>();

            if (obj is null)
            {
                results.Add(new(ConfigurationPath.Combine(path), null));

                return results;
            }

            var type = obj.GetType();

            if (type.IsSimple())
            {
                results.Add(new(ConfigurationPath.Combine(path), obj as string));
                return results;
            }

            if (type.IsDictionary())
            {
                foreach (DictionaryEntry item in (IDictionary)obj)
                {
                    path.Add(item.Key.ToString()!);
                    if (item.Value?.GetType().IsSimple() ?? true)
                    {
                        results.Add(new(ConfigurationPath.Combine(path), item.Value is DateTime dt ? dt.ToString("s") : item.Value?.ToString()));
                    }
                    else
                    {
                        var subitems = ToSettingItems(item, path, logger);
                        results.AddRange(subitems);
                    }
                    path.Remove(path.Last());


                }

                return results;
            }

            if (type.IsEnumerable())
            {
                var i = 0;

                foreach (var item in (IEnumerable)obj)
                {
                    path.Add(i.ToString());
                    if (item.GetType().IsSimple())
                    {
                        if (item is not null)
                        {
                            results.Add(new(ConfigurationPath.Combine(path), item is DateTime dt ? dt.ToString("s") : item.ToString()));
                        }
                    }
                    else
                    {

                        var subitems = ToSettingItems(item, path, logger);
                        results.AddRange(subitems);
                    }
                    path.Remove(path.Last());


                    i++;
                }

                return results;
            }

            foreach (var prop in type.GetOptionableProperties())
            {
                try
                {
                    if (obj is not DictionaryEntry || prop.Name != "Value")
                    {
                        path.Add(prop.Name);
                    }

                    if (prop.GetValue(obj) is object value)
                    {

                        if (prop.PropertyType.IsSimple())
                        {
                            results.Add(new(ConfigurationPath.Combine(path), value is DateTime dt ? dt.ToString("s") : value.ToString()));
                        }
                        else
                        {
                            results.AddRange(ToSettingItems(value, path, logger));
                        }
                    }
                    else
                    {
                        results.Add(new(ConfigurationPath.Combine(path), null)); // { MarkedForDeletion = true });
                    }

                    if (obj is not DictionaryEntry || prop.Name != "Value")
                    {
                        path.Remove(path.Last());
                    }
                }
                catch (TargetParameterCountException)
                {
                    if (logger is not null)
                    {
                        logger.LogWarning("Unable to gather this value from {type}", prop.Name);
                    }
                }
            }

            return results;

        }

    }
}
