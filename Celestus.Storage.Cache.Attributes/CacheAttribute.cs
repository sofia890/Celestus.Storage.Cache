using System;

namespace Celestus.Storage.Cache.Attributes
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class CacheAttribute : Attribute
    {
        public const int DEFAULT_TIMEOUT = 1000;
        public const int DEFAULT_DURATION = 1000;

        public int Timeout { get; private set; }
        public int Duration { get; private set; }
        public string Key { get; private set; }
        public bool EnableFilePersistence { get; private set; }

        public CacheAttribute(
            int timeoutInMilliseconds = DEFAULT_TIMEOUT,
            int durationInMs = DEFAULT_DURATION,
            string key = "",
            bool enableFilePersistence = false)
        {
            Timeout = timeoutInMilliseconds;
            Duration = durationInMs;
            Key = key;
            EnableFilePersistence = enableFilePersistence;
        }
    }
}
