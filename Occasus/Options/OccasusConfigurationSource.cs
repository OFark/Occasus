using Occasus.Repository.Interfaces;

namespace Occasus.Options
{
    public class OccasusConfigurationSource : IConfigurationSource
    {
        internal IOptionsStorageRepository StorageRepository { get; set; } = default!;
        public ISettingStorageWatcher SettingStorageWatcher { get; set; } = default!;

        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return new OccasusConfigurationProvider(StorageRepository);
        }
    }
}
