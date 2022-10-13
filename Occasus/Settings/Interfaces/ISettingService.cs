using Microsoft.Extensions.Options;
using Occasus.Settings.Models;

namespace Occasus.Settings.Interfaces;

public interface ISettingService
{
    IEnumerable<SettingBox> GetSettings();
    Task<ValidateOptionsResult> PersistSettingToStorage(SettingBox setting, CancellationToken token);
    Task ReloadAllSettings(CancellationToken? token = null);
    Task ClearAllSettings(CancellationToken? token = null);
}
