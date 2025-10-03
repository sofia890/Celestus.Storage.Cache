namespace Celestus.Storage.Cache
{
    public record FactoryEntry<CacheIdType, CacheKeyType, CacheType>(
        WeakReference<CacheType> CacheReference,
        CacheCleanerBase<CacheIdType, CacheKeyType> Cleaner,
        CacheIdType Key)
            where CacheIdType : class
            where CacheKeyType : class
            where CacheType : CacheBase<CacheIdType, CacheKeyType>;

    public class CacheManagerCleaner<IdType, CacheKeyType, CacheType> : IDisposable
        where IdType : class
        where CacheKeyType : class
        where CacheType : CacheBase<IdType, CacheKeyType>
    {
        public const int DEFAULT_INTERVAL_IN_MS = 10000;
        public const int STOP_TIMEOUT = 30000;

        readonly Queue<FactoryEntry<IdType, CacheKeyType, CacheType>> _elements = [];
        WeakReference<CacheManagerBase<IdType, CacheKeyType, CacheType>>? _cacheManager;

        TimeSpan _cleanupInterval = TimeSpan.FromMilliseconds(DEFAULT_INTERVAL_IN_MS);
        private bool _isDisposed;
        private bool _abort = false;
        readonly Task _cleanerLoop;
        readonly CancellationTokenSource cleanerLoopCancellationTokenSource = new();

        public CacheManagerCleaner()
        {
            _cleanerLoop = Task.Run(Cleanup, cleanerLoopCancellationTokenSource.Token);
        }

        public async Task Cleanup()
        {
            while (!_abort)
            {
                var remainingElements = new List<FactoryEntry<IdType, CacheKeyType, CacheType>>();

                while (_elements.TryDequeue(out var entry) && !_abort)
                {
                    if (!entry.CacheReference.TryGetTarget(out var cache))
                    {
                        entry.Cleaner.Dispose();
                    }
                    else if (cache.IsDisposed)
                    {
                        if (_cacheManager?.TryGetTarget(out var manager) ?? false)
                        {
                            manager.CacheExpired(cache.Id);
                        }
                        else
                        {
                            _elements.Enqueue(entry);

                            break;
                        }
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

                try
                {
                    await Task.Delay(_cleanupInterval, cleanerLoopCancellationTokenSource.Token);
                }
                catch (OperationCanceledException)
                {
                    // Do nothing
                }
            }
        }

        public void MonitorElement(CacheType cache)
        {
            _elements.Enqueue(new(new(cache), cache.Cleaner, cache.Id));
        }

        public void RegisterManager(WeakReference<CacheManagerBase<IdType, CacheKeyType, CacheType>> manager)
        {
            _cacheManager = manager;
        }

        public void SetCleanupInterval(TimeSpan interval)
        {
            _cleanupInterval = interval;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    _abort = true;

                    if (!_cleanerLoop.Wait(STOP_TIMEOUT))
                    {
                        try
                        {
                            cleanerLoopCancellationTokenSource.Cancel();
                        }
                        catch (OperationCanceledException)
                        {
                        }
                    }

                    _cleanerLoop.Dispose();
                    cleanerLoopCancellationTokenSource.Dispose();
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
