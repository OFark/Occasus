using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Occasus.Options;
using System.Diagnostics.CodeAnalysis;

namespace Occasus.Repository.Interfaces;

public interface IOptionsStorageRepositoryWithServices : IOptionsStorageRepository
{
    [NotNull]
    IServiceCollection? Services { get; }
}

public interface IOptionsStorageRepository
{
    IOptionsStorageRepositoryWithServices AddServices(IServiceCollection services);

    List<string>? Messages { get; }

    Task ClearSettings(string? classname = null, CancellationToken cancellation = default);   
    IDictionary<string, string> LoadSettings();
    Task ReloadSettings(CancellationToken cancellation = default);

    Task StoreSetting<T>(T value, Type valueType, CancellationToken cancellation = default);
    IChangeToken Watch();

}
