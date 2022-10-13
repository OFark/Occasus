using Microsoft.Extensions.Primitives;

namespace Occasus.Repository.Interfaces;

public interface IOptionsStorageRepository
{
    WebApplicationBuilder Builder { get; }

    Task ClearSettings(string? classname = null, CancellationToken? token = null);

    Task DeleteSetting(string key, CancellationToken? token = null);
    IDictionary<string, string> LoadSettings();
    Task ReloadSettings(CancellationToken? token = null);

    Task StoreSetting(string key, string value, CancellationToken? token = null);
    IChangeToken Watch();
}
