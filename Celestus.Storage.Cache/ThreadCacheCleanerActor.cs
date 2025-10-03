using Celestus.Exceptions;
using System.Text.Json;
using System.Threading.Channels;

namespace Celestus.Storage.Cache
{
    internal class ThreadCacheCleanerActor<CacheIdType, CacheKeyType> : IDisposable
        where CacheIdType : notnull
        where CacheKeyType : notnull
    {
        public const int DEFAULT_TIMEOUT_IN_MS = 5000;
        public const int STOP_TIMEOUT = 30000;

        long _cleanupIntervalInTicks;
        long _nextCleanupOpportunityInTicks;
        WeakReference<CacheBase<CacheIdType, CacheKeyType>>? _cacheReference = null;
        private bool _disposed = false;
        private readonly Task _signalHandlerTask;

        public Channel<Signal> CleanerPort { get; init; } = Channel.CreateUnbounded<Signal>(
            options: new UnboundedChannelOptions()
            {
                SingleReader = true,
                SingleWriter = false
            }
        );

        public ThreadCacheCleanerActor(TimeSpan interval)
        {
            _cleanupIntervalInTicks = interval.Ticks;
            _nextCleanupOpportunityInTicks = DateTime.UtcNow.Ticks + _cleanupIntervalInTicks;
            _signalHandlerTask = Task.Run(HandleSignals);
        }

        private Task NewTimeoutTask()
        {
            return Task.Delay(TimeSpan.FromTicks(_cleanupIntervalInTicks));
        }

        private async Task HandleSignals()
        {
            Task<Signal>? signalTask = null;

            var reader = CleanerPort.Reader;

            while (!reader.Completion.IsCompleted && !_disposed)
            {
                if (signalTask == null)
                {
                    signalTask = reader.ReadAsync().AsTask();
                }
                else if (await Task.WhenAny(signalTask, NewTimeoutTask()) != signalTask)
                {
                    Prune(DateTime.UtcNow.Ticks);
                }
                else if (signalTask.IsCompletedSuccessfully)
                {
                    Signal rawSignal = signalTask.Result;
                    signalTask = null;

                    switch (rawSignal.SignalId)
                    {
                        default:
                        case CleanerProtocol.Stop:
                            return;

                        case CleanerProtocol.RegisterCacheInd when rawSignal is RegisterCacheInd<CacheIdType, CacheKeyType> payload:
                            _cacheReference = payload.Cache;
                            break;

                        case CleanerProtocol.UnregisterCacheInd:
                            _cacheReference = null;
                            break;

                        case CleanerProtocol.ResetInd when rawSignal is ResetInd payload:
                            _cleanupIntervalInTicks = payload.CleanupIntervalInTicks;
                            _nextCleanupOpportunityInTicks = DateTime.UtcNow.Ticks + _cleanupIntervalInTicks;
                            break;
                    }

                    Prune(DateTime.UtcNow.Ticks);
                }
            }
        }

        private void Prune(long timeInTicks)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);

            if (_nextCleanupOpportunityInTicks > timeInTicks)
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
                List<CacheKeyType> expiredKeys = [];

                var reader = CleanerPort.Reader;

                foreach (var entry in cache.Storage)
                {
                    if (reader.Completion.IsCompleted)
                    {
                        return;
                    }

                    if (CacheCleaner<CacheIdType, CacheKeyType>.ExpiredCriteria(entry.Value, timeInTicks))
                    {
                        expiredKeys.Add(entry.Key);
                    }
                }

                if (expiredKeys.Count > 0)
                {
                    _ = cache.TryRemove([.. expiredKeys]);
                }

                _nextCleanupOpportunityInTicks = timeInTicks + _cleanupIntervalInTicks;
            }
        }

        public void ReadSettings(ref Utf8JsonReader reader)
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
                            case nameof(_cleanupIntervalInTicks):
                                _ = reader.Read();

                                var cleanupIntervalInTicks = reader.GetInt64();
                                CleanerPort.Writer.TryWrite(new ResetInd(cleanupIntervalInTicks));

                                intervalValueFound = true;
                                break;

                            default:
                                break;

                        }
                        break;
                }
            }

        End:
            Condition.ThrowIf<MissingValueJsonException>(!intervalValueFound, nameof(_cleanupIntervalInTicks));
        }

        public void WriteSettings(Utf8JsonWriter writer)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);

            writer.WriteStartObject();
            writer.WritePropertyName(nameof(_cleanupIntervalInTicks));
            writer.WriteNumberValue(_cleanupIntervalInTicks);
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
                    _ = _signalHandlerTask.Wait(STOP_TIMEOUT);
                    _signalHandlerTask.Dispose();
                }

                _disposed = true;
            }
        }

        public bool IsDisposed => _disposed;
        #endregion
    }
}
