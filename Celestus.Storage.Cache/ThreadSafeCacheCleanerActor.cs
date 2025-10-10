using Celestus.Exceptions;
using System.Text.Json;
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
        DateTime _nextCleanupOpportunity;
        WeakReference<CacheBase<CacheIdType, CacheKeyType>>? _cacheReference = null;
        private bool _disposed = false;
        private readonly Task _signalHandlerTask;
        readonly CancellationTokenSource cleanerLoopCancellationTokenSource = new();

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
            _nextCleanupOpportunity = DateTime.UtcNow + _cleanupInterval;
            _signalHandlerTask = Task.Run(HandleSignals);
        }

        private void HandleSignals()
        {
            var cancelToken = cleanerLoopCancellationTokenSource.Token;

            Task<Signal>? signalTask = null;

            var reader = CleanerPort.Reader;

            while (!reader.Completion.IsCompleted && !_disposed)
            {
                cancelToken.ThrowIfCancellationRequested();

                if (signalTask == null)
                {
                    signalTask = reader.ReadAsync().AsTask();
                }
                else if (signalTask.Wait(_cleanupInterval))
                {
                    if (signalTask.IsCompletedSuccessfully)
                    {
                        Signal rawSignal = signalTask.Result;
                        signalTask = null;

                        switch (rawSignal.SignalId)
                        {
                            default:
                                throw new UnknownSignalException($"Unknown signal ID '{rawSignal.SignalId}' encountered.");

                            case CleanerProtocol.Stop:
                                return;

                            case CleanerProtocol.RegisterCacheInd when rawSignal is RegisterCacheInd<CacheIdType, CacheKeyType> payload:
                                _cacheReference = payload.Cache;
                                break;

                            case CleanerProtocol.UnregisterCacheInd:
                                _cacheReference = null;
                                break;

                            case CleanerProtocol.ResetInd when rawSignal is ResetInd payload:
                                _cleanupInterval = payload.CleanupInterval;
                                _nextCleanupOpportunity = DateTime.UtcNow + _cleanupInterval;
                                break;
                        }
                    }
                    else
                    {
                        signalTask = null;
                    }

                    Prune(DateTime.UtcNow);

                }
                else
                {
                    Prune(DateTime.UtcNow);
                }
            }
        }

        private void Prune(DateTime now)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);

            if (_nextCleanupOpportunity > now)
            {
                return;
            }
            else if (_cacheReference == null ||
                     !_cacheReference.TryGetTarget(out var cache))
            {
                // Wait for reference to be available.
                return;
            }
            else
            {
                var cancelToken = cleanerLoopCancellationTokenSource.Token;

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

                _nextCleanupOpportunity = now + _cleanupInterval;
            }
        }

        public void ReadSettings(ref Utf8JsonReader reader, JsonSerializerOptions options)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);

            bool intervalValueFound = false;

            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    default:
                        break;

                    case JsonTokenType.EndObject:
                        goto End;

                    case JsonTokenType.StartObject:
                        break;

                    case JsonTokenType.PropertyName:
                        switch (reader.GetString())
                        {
                            case nameof(_cleanupInterval):
                                _ = reader.Read();

                                var cleanupInterval = JsonSerializer.Deserialize<TimeSpan>(ref reader, options);
                                CleanerPort.Writer.TryWrite(new ResetInd(cleanupInterval));

                                intervalValueFound = true;
                                break;

                            default:
                                break;

                        }
                        break;
                }
            }

        End:
            Condition.ThrowIf<MissingValueJsonException>(!intervalValueFound, nameof(_cleanupInterval));
        }

        public void WriteSettings(Utf8JsonWriter writer, JsonSerializerOptions options)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);

            writer.WriteStartObject();
            writer.WritePropertyName(nameof(_cleanupInterval));
            JsonSerializer.Serialize(writer, _cleanupInterval, options);
            writer.WriteEndObject();
        }

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
                    CleanerPort.Writer.Complete();

                    if (!_signalHandlerTask.Wait(STOP_TIMEOUT))
                    {
                        try
                        {
                            cleanerLoopCancellationTokenSource.Cancel();
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

                    cleanerLoopCancellationTokenSource.Dispose();
                }

                _disposed = true;
            }
        }

        public bool IsDisposed => _disposed;
        #endregion
    }
}
