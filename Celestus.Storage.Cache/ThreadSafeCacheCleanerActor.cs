using System.Threading.Channels;

namespace Celestus.Storage.Cache
{
    class UnknownSignalException(string message) : Exception(message);

    internal class ThreadSafeCacheCleanerActor<CacheIdType, CacheKeyType> : IDisposable
        where CacheIdType : notnull
        where CacheKeyType : notnull
    {
        public const int DEFAULT_TIMEOUT_IN_MS = 5000;
        public const int STOP_TIMEOUT = 30000;
        public const int EXPECTED_MAX_CONCURRENT_SIGNALS = 10;

        TimeSpan _cleanupInterval;
        WeakReference<ICacheBase<CacheIdType, CacheKeyType>>? _cacheReference = null;
        private bool _disposed = false;
        private readonly Task _signalHandlerTask;
        readonly CancellationTokenSource _cleanerLoopCancellationTokenSource = new();

        public Channel<Signal> CleanerPort { get; init; } = Channel.CreateBounded<Signal>(
            options: new BoundedChannelOptions(EXPECTED_MAX_CONCURRENT_SIGNALS)
            {
                SingleReader = true,
                SingleWriter = false
            }
        );

        public ThreadSafeCacheCleanerActor(TimeSpan interval)
        {
            _cleanupInterval = interval;
            _signalHandlerTask = Task.Run(HandleSignals);
        }

        private async Task HandleSignals()
        {
            var cancelToken = _cleanerLoopCancellationTokenSource.Token;

            Task<Signal>? signalwaitTask = null;

            Task? pruneIntervalTask = null;

            CancellationTokenSource pruneCancellationTokenSource = new();

            var reader = CleanerPort.Reader;

            while (!reader.Completion.IsCompleted && !_disposed)
            {
                if (cancelToken.IsCancellationRequested)
                {
                    CancelPruneTask(pruneIntervalTask, pruneCancellationTokenSource);
                }

                cancelToken.ThrowIfCancellationRequested();

                signalwaitTask ??= reader.ReadAsync(cancelToken).AsTask();
                pruneIntervalTask ??= Task.Delay(_cleanupInterval, pruneCancellationTokenSource.Token);

                var completed = await Task.WhenAny(signalwaitTask, pruneIntervalTask);

                if (completed == signalwaitTask && signalwaitTask.IsCompletedSuccessfully)
                {
                    var signal = signalwaitTask.Result;

                    signalwaitTask = null;

                    switch (signal.SignalId)
                    {
                        default:
                            throw new UnknownSignalException($"Unknown signal ID '{signal.SignalId}' encountered.");

                        case CleanerProtocol.Stop:
                            return;

                        case CleanerProtocol.RegisterCacheInd when signal is RegisterCacheInd<CacheIdType, CacheKeyType> payload:
                            _cacheReference = payload.Cache;
                            break;

                        case CleanerProtocol.UnregisterCacheInd:
                            _cacheReference = null;
                            break;

                        case CleanerProtocol.ResetInd when signal is ResetInd payload:
                            _cleanupInterval = payload.CleanupInterval;

                            CancelPruneTask(pruneIntervalTask, pruneCancellationTokenSource);
                            pruneCancellationTokenSource.Dispose();
                            pruneCancellationTokenSource = new();

                            pruneIntervalTask = null;
                            break;
                    }
                }
                else if (completed == signalwaitTask && signalwaitTask.IsFaulted)
                {
                    signalwaitTask = null;
                }
                else if (completed == pruneIntervalTask && pruneIntervalTask.IsCompletedSuccessfully)
                {
                    pruneIntervalTask = null;

                    Prune(DateTime.UtcNow, cancelToken);
                }
                else if (completed == pruneIntervalTask && pruneIntervalTask.IsFaulted)
                {
                    pruneIntervalTask = null;
                }
            }
        }

        private static void CancelPruneTask(Task? pruneIntervalTask, CancellationTokenSource pruneCancellationTokenSource)
        {
            if (pruneIntervalTask != null)
            {
                try
                {
                    pruneCancellationTokenSource.Cancel();
                    pruneIntervalTask.Wait();
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
            }
        }

        private void Prune(DateTime now,  CancellationToken cancelToken)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);

            if (_cacheReference == null ||
                !_cacheReference.TryGetTarget(out var cache))
            {
                // Wait for reference to be available.
                return;
            }
            else
            {
                List<CacheKeyType> expiredKeys = [];

                var reader = CleanerPort.Reader;

                foreach (var entry in cache.GetEntries())
                {
                    cancelToken.ThrowIfCancellationRequested();

                    if (reader.Completion.IsCompleted)
                    {
                        return;
                    }

                    if (CacheCleaner<CacheIdType, CacheKeyType>.ExpiredCriteria(entry.Value, now))
                    {
                        expiredKeys.Add(entry.Key);
                    }
                }

                if (expiredKeys.Count > 0)
                {
                    _ = cache.TryRemove([.. expiredKeys]);
                }
            }
        }

        public TimeSpan GetCleaningInterval() => _cleanupInterval;

        public void SetCleaningInterval(TimeSpan interval) => _ = CleanerPort.Writer.TryWrite(new ResetInd(interval));

        #region IDisposable
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    CleanerPort.Writer.TryWrite(new StopInd());

                    if (!_signalHandlerTask.Wait(STOP_TIMEOUT))
                    {
                        try
                        {
                            _cleanerLoopCancellationTokenSource.Cancel();

                            CleanerPort.Writer.TryWrite(new StopInd());
                            CleanerPort.Writer.Complete();

                            _signalHandlerTask.Wait();
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
                    }

                    _signalHandlerTask.Dispose();

                    _cleanerLoopCancellationTokenSource.Dispose();
                }

                _disposed = true;
            }
        }

        public bool IsDisposed => _disposed;
        #endregion
    }
}
