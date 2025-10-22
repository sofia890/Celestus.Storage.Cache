namespace Celestus.Storage.Cache
{
    public partial class Cache
    {
        public static CacheManager Factory { get; } = new();

        public class CacheManager : CacheManagerBase<string, string, Cache>
        {
            #region CacheManagerBase
            protected override Cache? TryCreateFromFile(
                FileInfo file,
                BlockedEntryBehavior behaviourMode = BlockedEntryBehavior.Throw,
                CacheTypeFilterMode filterMode = CacheTypeFilterMode.Blacklist,
                IEnumerable<Type>? types = null)
            {
                return Cache.TryCreateFromFile(file, behaviourMode, filterMode, types);
            }

            protected override bool Update(Cache from, Cache to, TimeSpan? timeout)
            {
                // Make a copy to sever any direct connection.
                to._storage = from._storage.ToDictionary();
                to.BlockedEntryBehavior = from.BlockedEntryBehavior;
                to.TypeRegister = (CacheTypeRegister)from.TypeRegister.Clone();

                return true;
            }
            #endregion
        }
    }
}
