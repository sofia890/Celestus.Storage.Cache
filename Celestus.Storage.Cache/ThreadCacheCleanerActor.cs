using Celestus.Exceptions;
using System.Reflection.PortableExecutable;
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

        TimeSpan _cleanupInterval;
        DateTime _nextCleanupOpportunity;
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
            _cleanupInterval = interval;
            _nextCleanupOpportunity = DateTime.UtcNow + _cleanupInterval;
            _signalHandlerTask = Task.Run(HandleSignals);
        }

        private Task NewTimeoutTask()
        {
            return Task.Delay(_cleanupInterval);
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
                    Prune(DateTime.UtcNow);
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
                            _cleanupInterval = payload.CleanupInterval;
                            _nextCleanupOpportunity = DateTime.UtcNow + _cleanupInterval;
                            break;
                    }

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
                List<CacheKeyType> expiredKeys = [];

                var reader = CleanerPort.Reader;

                foreach (var entry in cache.Storage)
                {
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
                    _ = _signalHandlerTask.Wait(STOP_TIMEOUT);

                    try
                    {
                        _signalHandlerTask.Dispose();
                    }
                    catch (InvalidOperationException)
                    {
                        // Ignore, task already stopped.
                    }
                }

                _disposed = true;
            }
        }

        public bool IsDisposed => _disposed;
        #endregion
    }
}
