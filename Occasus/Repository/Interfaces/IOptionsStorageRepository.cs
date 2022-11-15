using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;

namespace Occasus.Repository.Interfaces;

public interface IOptionsStorageRepository
{
    IServiceCollection Services { get; }
    IConfiguration Configuration { get; }
    Task ClearSettings(string? classname = null, CancellationToken cancellation = default);   
    IDictionary<string, string> LoadSettings();
    Task ReloadSettings(CancellationToken cancellation = default);

    Task StoreSetting<T>(T value, Type valueType, CancellationToken cancellation = default);
    IChangeToken Watch();
}
