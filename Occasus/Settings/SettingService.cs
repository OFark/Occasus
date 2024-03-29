﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Occasus.Settings.Interfaces;
using Occasus.Settings.Models;

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

        public IEnumerable<SettingBox> GetSettings()
        {

            foreach (var settingBox in SettingsStore.SettingsWithRepositories)
            {
                settingBox.LoadValueFromConfiguration(configuration);
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
            var valid = Validate(setting);
            if (!valid.Failed)
            {
                await setting.PersistSettingToStorageAsync(cancellation).ConfigureAwait(false);
            }

            return valid;
        }


        private ValidateOptionsResult Validate(SettingBox setting) //This does not validate annotations
        {
            var validator = serviceProvider.GetService(typeof(IValidateOptions<>).MakeGenericType(setting.Type)) as dynamic;

            var method = validator?.GetType().GetMethod("Validate", new Type[] { typeof(string), setting.Type });

            var valid = method?.Invoke(validator, new object?[] { string.Empty, setting.Value });

            return valid ?? ValidateOptionsResult.Skip;

        }
    }
}
