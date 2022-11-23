using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Occasus.Repository.Interfaces;

namespace Occasus.Options
{
    public abstract class SettingsRepositoryBase : IOptionsStorageRepositoryWithServices
    {
        public List<string>? Messages { get; protected set; }

        public IServiceCollection Services { get => _services ?? throw new Exception("Services should be accessed via the IOptionsStorageRepositoryWithServices interface"); }

        protected IServiceCollection? _services;

        public virtual IOptionsStorageRepositoryWithServices AddServices(IServiceCollection services)
        {
            _services = services;
            Logger = services.BuildServiceProvider().GetService<ILogger<SettingsRepositoryBase>>();
            return this;
        }
        public ILogger? Logger { get; private set; }

        public abstract Task ClearSettings(string? classname = null, CancellationToken cancellation = default);

        public abstract IDictionary<string, string> LoadSettings();

        public abstract Task ReloadSettings(CancellationToken cancellation = default);

        public abstract Task StoreSetting<T>(T value, Type valueType, CancellationToken cancellation = default);

        protected ConfigurationReloadToken _reloadToken = new();

        public virtual IChangeToken Watch()
        {
            return _reloadToken;
        }


        protected void OnReload()
        {
            ConfigurationReloadToken previousToken = Interlocked.Exchange(ref _reloadToken, new ConfigurationReloadToken());
            previousToken.OnReload();
        }
    }
}
