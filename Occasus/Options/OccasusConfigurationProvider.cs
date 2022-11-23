using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using Occasus.Repository.Interfaces;
using System.Diagnostics;

namespace Occasus.Options
{
    public sealed class OccasusConfigurationProvider : ConfigurationProvider, IDisposable
    {
        private readonly IOptionsStorageRepository storageRepository;
        private IDisposable _changeTokenRegistration;

        public OccasusConfigurationProvider(IOptionsStorageRepository storageRepository)
        {
            this.storageRepository = storageRepository;

            //_changeTokenRegistration = this.storageRepository.Watch(GetReloadToken()).RegisterChangeCallback(ReloadData, null);
            ChangeToken.OnChange<IOptionsStorageRepository>(() => this.storageRepository.Watch(), ReloadData, this.storageRepository);
        }

        public void Dispose()
        {
            _changeTokenRegistration.Dispose();
        }

        public override void Load()
        {
            Data = storageRepository.LoadSettings();
            
            Console.WriteLine("Loading Data");

            Reload();

            Debug.Assert(Data is not null);
        }

        internal void Reload()
        {
            OnReload();
        }

        private void ReloadData(object x)
        {
            Load();
        }
    }
}
