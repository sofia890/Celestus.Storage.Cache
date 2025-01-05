using System;

namespace Celestus.Storage.Cache.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class CacheAttribute : Attribute
    {
        public const int DEFAULT_TIMEOUT = 1000;

        public object Timeout
        {
            get;
            private set;
        }

        public CacheAttribute(int timeoutInMilliseconds = DEFAULT_TIMEOUT)
        {
            Timeout = timeoutInMilliseconds;
        }
    }
}
