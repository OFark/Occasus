using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using Occasus.Repository.Interfaces;
using System.Diagnostics;

namespace Occasus.Options
{
    public sealed class OccasusConfigurationProvider : ConfigurationProvider, IDisposable
    {
        private readonly IOptionsStorageRepository storageRepository;
        private readonly IDisposable _changeTokenRegistration;

        public OccasusConfigurationProvider(IOptionsStorageRepository storageRepository)
        {
            this.storageRepository = storageRepository;

            _changeTokenRegistration = ChangeToken.OnChange(() => storageRepository.Watch(), Load);
        }


        public void Dispose()
        {
            _changeTokenRegistration.Dispose();

        }

        public override void Load()
        {
            Data = storageRepository.LoadSettings();

            OnReload();

            Debug.Assert(Data is not null);
        }
    }
}
