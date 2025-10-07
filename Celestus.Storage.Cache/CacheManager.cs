namespace Celestus.Storage.Cache
{
    public partial class Cache
    {
        public static CacheManager Factory { get; } = new();

        public class CacheManager : CacheManagerBase<string, string, Cache>
        {
            #region CacheManagerBase
            protected override Cache? TryCreateFromFile(FileInfo file)
            {
                return Cache.TryCreateFromFile(file);
            }

            protected override bool Update(Cache from, Cache to, TimeSpan? timeout)
            {
                // Make a copy to sever any direct connection.
                to.Storage = from.Storage.ToDictionary();

                return true;
            }
            #endregion
        }
    }
}
