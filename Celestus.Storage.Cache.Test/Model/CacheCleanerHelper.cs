using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celestus.Storage.Cache.Test.Model
{
    public static class CacheCleanerHelper
    {
        public static CacheCleanerBase<string> GetCleaner(Type cleanerTypeToTest, int cleanupIntervalInMs)
        {
            if (cleanerTypeToTest == typeof(CacheCleaner<string>))
            {
                return new CacheCleaner<string>(cleanupIntervalInMs);
            }
            else if (cleanerTypeToTest == typeof(ThreadCacheCleaner<string>))
            {
                return new ThreadCacheCleaner<string>(cleanupIntervalInMs);
            }
            else
            {
                throw new ArgumentException("Invalid type to test", nameof(cleanerTypeToTest));
            }
        }
    }
}
