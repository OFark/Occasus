using Microsoft.Extensions.Options;
using Occasus.Settings.Models;

namespace Occasus.Settings.Interfaces;

public interface ISettingService
{
    IEnumerable<SettingBox> GetSettings();
    Task<ValidateOptionsResult> PersistSettingToStorage(SettingBox setting, CancellationToken cancellation = default);
    Task ReloadAllSettings(CancellationToken cancellation = default);
    Task ClearAllSettings(CancellationToken cancellation = default);
    SettingBox ReloadFromConfiguration(SettingBox settingBox);
}
