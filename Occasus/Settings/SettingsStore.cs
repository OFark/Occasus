using Occasus.Repository.Interfaces;
using Occasus.Settings.Models;

namespace Occasus.Settings
{
    public static class SettingsStore
    {
        internal static List<SettingBox> Settings = new();

        internal static IEnumerable<SettingBox> SettingsWithRepositories => Settings.Where(s => s.HasRepository);

        internal static List<IOptionsStorageRepository> ActiveRepositories = new();

        internal static void Add<T>(IOptionsStorageRepository optionsStorageRepository) where T : class => Settings.Add(new(typeof(T), optionsStorageRepository));

        public static bool TryAdd(IOptionsStorageRepository storageRepository)
        {
            if (!ActiveRepositories.Contains(storageRepository))
            {
                ActiveRepositories.Add(storageRepository);
                return true;
            }

            return false;
        }
    }
}
