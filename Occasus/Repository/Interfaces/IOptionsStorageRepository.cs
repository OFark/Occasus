using Microsoft.Extensions.Primitives;

namespace Occasus.Repository.Interfaces;

public interface IOptionsStorageRepository
{
    WebApplicationBuilder Builder { get; }

    Task ClearSettings(string? classname = null, CancellationToken cancellation = default);   
    IDictionary<string, string> LoadSettings();
    Task ReloadSettings(CancellationToken cancellation = default);

    Task StoreSetting<T>(T value, Type valueType, CancellationToken cancellation = default);
    IChangeToken Watch();
}
