using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Celestus.Storage.Cache
{
    public record FactoryEntry<CacheIdType, CacheKeyType, CacheType>(
        WeakReference<CacheType> CacheReference,
        CacheCleanerBase<CacheIdType, CacheKeyType> Cleaner,
        CacheIdType Key)
            where CacheIdType : class
            where CacheKeyType : class
            where CacheType : CacheBase<CacheIdType, CacheKeyType>;

    public class CacheManagerCleaner<CacheIdType, CacheKeyType, CacheType> : IDisposable
        where CacheIdType : class
        where CacheKeyType : class
        where CacheType : CacheBase<CacheIdType, CacheKeyType>
    {
        public const int DEFAULT_INTERVAL_IN_MS = 10000;
        public const int STOP_TIMEOUT = 30000;

        readonly ConcurrentQueue<FactoryEntry<CacheIdType, CacheKeyType, CacheType>> _elements = [];
        WeakReference<CacheManagerBase<CacheIdType, CacheKeyType, CacheType>>? _cacheManager;

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
            var cancelToken = cleanerLoopCancellationTokenSource.Token;

            while (!_abort)
            {
                cancelToken.ThrowIfCancellationRequested();

                var remainingElements = new List<FactoryEntry<CacheIdType, CacheKeyType, CacheType>>();

                while (_elements.TryDequeue(out var entry) && !_abort)
                {
                    cancelToken.ThrowIfCancellationRequested();

                    if (!entry.CacheReference.TryGetTarget(out var cache))
                    {
                        entry.Cleaner.Dispose();
                    }
                    else if (!_cacheManager?.TryGetTarget(out var manager) ?? true ||
                             !manager.RemoveIfExpired(cache.Id))
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

                await Task.Delay(_cleanupInterval, cleanerLoopCancellationTokenSource.Token);
            }
        }

        public void MonitorElement(CacheType cache)
        {
            _elements.Enqueue(new(new(cache), cache.Cleaner, cache.Id));
        }

        public void RegisterManager(WeakReference<CacheManagerBase<CacheIdType, CacheKeyType, CacheType>> manager)
        {
            _cacheManager = manager;
        }

        public TimeSpan GetCleanupInterval()
        {
            return _cleanupInterval;
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

                    try
                    {
                        cleanerLoopCancellationTokenSource.Cancel();
                        _cleanerLoop.Wait();
                    }
                    catch (AggregateException exception)
                    {
                        var exceptionType = exception.InnerException?.GetType();

                        if (exceptionType == typeof(TaskCanceledException) ||
                            exceptionType == typeof(OperationCanceledException))
                        {
                            // Ignore this exception
                        }
                        else
                        {
                            throw;
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // Ignore this exception
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
