namespace Celestus.Storage.Cache
{
    public partial class Cache
    {
        public static CacheManager Factory = new();

        public class CacheManager : CacheManagerBase<string, Cache>
        {
            #region CacheManagerBase
            protected override Cache? TryCreateFromFile(Uri path)
            {
                return Cache.TryCreateFromFile(path);
            }

            protected override bool Update(Cache from, Cache to, TimeSpan? timeout)
            {
                lock (this)
                {
                    // Make a copy to sever any direct connection.
                    to.Storage = from.Storage.ToDictionary();
                }

                return true;
            }
            #endregion
        }
    }
}
