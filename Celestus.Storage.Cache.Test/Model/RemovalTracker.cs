namespace Celestus.Storage.Cache.Test.Model
{
    internal class RemovalTracker
    {
        public AutoResetEvent EntryRemoved { get; private set; } = new(false);

        public List<string> RemovedKeys { get; private set; } = [];

        public bool TryRemove(List<string> keys)
        {
            RemovedKeys.AddRange(keys);

            EntryRemoved.Set();

            return true;
        }
    }
}
