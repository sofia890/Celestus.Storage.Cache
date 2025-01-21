using System;

namespace Celestus.Storage.Cache.Attributes
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class CacheAttribute : Attribute
    {
        public const int DEFAULT_TIMEOUT = 1000;

        public int Timeout
        {
            get;
            private set;
        }

        public string Key
        {
            get;
            private set;
        }

        public CacheAttribute(int timeoutInMilliseconds = DEFAULT_TIMEOUT, string key = "")
        {
            Timeout = timeoutInMilliseconds;
            Key = key;
        }
    }
}
