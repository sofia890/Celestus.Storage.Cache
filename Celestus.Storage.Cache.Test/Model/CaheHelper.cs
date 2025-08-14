namespace Celestus.Storage.Cache.Test.Model
{
	public static class CacheHelper
	{
		public static CacheBase<string> GetOrCreateShared(Type cacheType, string key)
		{
			if (typeof(ThreadCache) == cacheType)
			{
				return ThreadCache.Factory.GetOrCreateShared(key);
			}
			else
			{
				return Cache.Factory.GetOrCreateShared(key);
			}
		}
	}
}