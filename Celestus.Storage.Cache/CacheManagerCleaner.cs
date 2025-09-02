
using System.Collections.Concurrent;

namespace Celestus.Storage.Cache
{
    public record FactoryEntry<CacheKeyType, CacheType>(WeakReference<CacheType> CacheReference, CacheCleanerBase<CacheKeyType> Cleaner)
        where CacheKeyType : class
        where CacheType : CacheBase<CacheKeyType>;

    public class CacheManagerCleaner<ManagerKeyType, CacheKeyType, CacheType> : IDisposable
        where ManagerKeyType : class
        where CacheKeyType : class
        where CacheType : CacheBase<CacheKeyType>
    {
        public const int DEFAULT_INTERVAL_IN_MS = 10000;
        public const int A_MOMENT = 500;

        Queue<FactoryEntry<CacheKeyType, CacheType>> _elements = [];
        WeakReference<Action<string>>? _elementExpiredCallback;

        int cleanupIntervalInMilliseconds = TimeSpan.FromMilliseconds(DEFAULT_INTERVAL_IN_MS).Milliseconds;
        private bool _isDisposed;
        private bool _abort = false;
        readonly Task _cleanerLoop;

        public CacheManagerCleaner()
        {
            _cleanerLoop = Task.Run(Cleanup);
        }

        public async Task Cleanup()
        {
            while (!_abort)
            {
                var remainingElements = new List<FactoryEntry<CacheKeyType, CacheType>>();

                while (_elements.TryDequeue(out var entry) && !_abort)
                {
                    if (!entry.CacheReference.TryGetTarget(out var cache))
                    {
                        entry.Cleaner.Dispose();
                    }
                    else if (cache.IsDisposed)
                    {
                        // Need to cleanup dictionary.
                    }
                    else
                    {
                        remainingElements.Add(entry);
                    }
                }

                if (!_abort)
                {
                    foreach (var entry in remainingElements)
                    {
                        _elements.Enqueue(entry);
                    }
                }

                await Task.Delay(cleanupIntervalInMilliseconds);
            }
        }

        public void MonitorElement(CacheType cache)
        {
            _elements.Enqueue(new(new(cache), cache.Cleaner));
        }

        public void SetElementExpiredCallback(WeakReference<Action<string>> callback)
        {
            _elementExpiredCallback = callback;
        }

        public void SetCleanupInterval(TimeSpan interval)
        {
            cleanupIntervalInMilliseconds = interval.Milliseconds;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    _abort = true;

                    _cleanerLoop.Wait(A_MOMENT);
                    _cleanerLoop.Dispose();
                }

                _isDisposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
