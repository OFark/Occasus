using Microsoft.Extensions.Primitives;

namespace Occasus.Repository.Interfaces
{
    public interface ISettingStorageWatcher : IDisposable
    {
        IChangeToken Watch();
    }
}
