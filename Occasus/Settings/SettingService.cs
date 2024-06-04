using Humanizer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Occasus.Helpers;
using Occasus.Settings.Interfaces;
using Occasus.Settings.Models;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Occasus.Settings
{
    internal class SettingService : ISettingService
    {
        private readonly IConfiguration configuration;
        private readonly ILogger<SettingService> logger;
        private readonly IServiceProvider serviceProvider;

        public string? UIPassword { get; set; }

        public SettingService(IConfiguration configuration, ILogger<SettingService> logger, IServiceProvider serviceProvider)
        {
            this.configuration = configuration;
            this.logger = logger;
            this.serviceProvider = serviceProvider;
        }

        public async Task ClearAllSettings(CancellationToken cancellation = default)
        {
            foreach (var set in SettingsStore.SettingsWithRepositories)
            {
                await set.ClearSettingStorageAsync(cancellation).ConfigureAwait(false);
            }
        }

        public SettingBox ReloadFromConfiguration(SettingBox settingBox)
        {
            settingBox.LoadValueFromConfiguration(configuration);
            return settingBox;
        }

        public IList<SettingBox> GetSettings()
        {

            foreach (var settingBox in SettingsStore.SettingsWithRepositories)
            {
                settingBox.LoadValueFromConfiguration(configuration);
                settingBox.ValidationResult = Validate(settingBox);
            }

            return SettingsStore.Settings;
        }

        public async Task ReloadAllSettings(CancellationToken cancellation = default)
        {
            foreach (var repo in SettingsStore.ActiveRepositories)
            {
                await repo.ReloadSettings(cancellation).ConfigureAwait(false);
            }
        }

        public async Task<ValidateOptionsResult> PersistSettingToStorage(SettingBox setting, CancellationToken cancellation = default)
        {
            setting.ValidationResult = Validate(setting);
            if (setting.IsValid)
            {
                await setting.PersistSettingToStorageAsync(cancellation).ConfigureAwait(false);
            }
            return setting.ValidationResult;
        }


        private ValidateOptionsResult Validate(SettingBox setting)
        {
            var validator = serviceProvider.GetService(typeof(IValidateOptions<>).MakeGenericType(setting.Type)) as dynamic;

            var method = validator?.GetType().GetMethod("Validate", new Type[] { typeof(string), setting.Type });

            ValidateOptionsResult? valid = method?.Invoke(validator, new object?[] { string.Empty, setting.Value });

            if (valid is null || valid.Succeeded)
            {
                valid = ValidateRequiredProperties(setting.Type, setting.Value);
            }

            return valid ?? ValidateOptionsResult.Skip;
        }

        private static ValidateOptionsResult ValidateRequiredProperties(Type type, object value)
        {
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            var somethingWasRequired = false;

            var failures = new List<string>();

            foreach (var prop in properties)
            {
                var required = Attribute.GetCustomAttribute(prop, typeof(RequiredAttribute)) is not null;

                if (!required || !prop.CanWrite)
                {
                    continue;
                }

                somethingWasRequired = true;

                var propValue = prop.GetValue(value);

                if (propValue is null)
                {
                    failures.Add($"{prop.Name.Humanize()} is required");
                }

                if (propValue is not null &&
                    !prop.PropertyType.IsSimple() &&
                    !prop.PropertyType.IsCollection() &&
                    !prop.PropertyType.IsDictionary() &&
                    prop.PropertyType.GetProperties(BindingFlags.Public | BindingFlags.Instance).Length != 0)
                {
                    var propResponse = ValidateRequiredProperties(prop.PropertyType, propValue);
                    if(propResponse.Failed)
                    {
                        failures.AddRange(propResponse.Failures);
                    }
                }
            }

            return somethingWasRequired ?
                failures.Count > 0 ?
                    ValidateOptionsResult.Fail(failures) :
                    ValidateOptionsResult.Success :
                ValidateOptionsResult.Skip;
        }
    }
}
