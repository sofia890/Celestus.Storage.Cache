using System.Text.Json;
using System.Threading.Channels;

namespace Celestus.Storage.Cache
{
    internal class ThreadCacheCleanerActor<KeyType> : IDisposable
        where KeyType : notnull
    {
        public const int DEFAULT_TIMEOUT_IN_MS = 5000;

        long _cleanupIntervalInTicks;
        long _nextCleanupOpportunityInTicks = 0;
        WeakReference<Func<List<KeyType>, bool>> _removalCallbackReference = new((keys) => false);
        WeakReference<IEnumerable<KeyValuePair<KeyType, CacheEntry>>> _collectionReference = new(new Dictionary<KeyType, CacheEntry>());
        private bool _disposed = false;
        private readonly Task _signalHandlerTask;

        public Channel<Signal> CleanerPort { get; init; } = Channel.CreateUnbounded<Signal>(
            options: new UnboundedChannelOptions()
            {
                SingleReader = true,
                SingleWriter = false
            }
        );

        public ThreadCacheCleanerActor(int cleanupIntervalInMs)
        {
            _cleanupIntervalInTicks = TimeSpan.FromMilliseconds(cleanupIntervalInMs).Ticks;
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

                    if (_disposed) break;

                    switch (rawSignal.SignalId)
                    {
                        default:
                            break;

                        case CleanerProtocol.RegisterRemovalCallbackInd when rawSignal is RegisterRemovalCallbackInd<KeyType> payload:
                            _removalCallbackReference = payload.Callback;
                            break;

                        case CleanerProtocol.Registercollection when rawSignal is RegistercollectionInd<KeyType> payload:
                            _collectionReference = payload.collection;
                            break;

                        case CleanerProtocol.ResetInd when rawSignal is ResetInd payload:
                            _cleanupIntervalInTicks = payload.CleanupIntervalInTicks;
                            _nextCleanupOpportunityInTicks = 0;
                            break;
                    }
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
            else if (!_collectionReference.TryGetTarget(out var collection))
            {
                // Wait for reference to be available.
                return;
            }
            else
            {
                List<KeyType> expiredKeys = [];

                foreach (var entry in collection)
                {
                    if (CacheCleaner<KeyType>.ExpiredCriteria(entry.Value, timeInTicks))
                    {
                        expiredKeys.Add(entry.Key);
                    }
                }

                if (expiredKeys.Count > 0 && _removalCallbackReference.TryGetTarget(out var callback))
                {
                    _ = callback(expiredKeys);
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
            if (!intervalValueFound)
            {
                throw new MissingValueJsonException(nameof(_cleanupIntervalInTicks));
            }
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
                    _ = CleanerPort.Writer.TryComplete();

                    _signalHandlerTask.Wait(TimeSpan.FromSeconds(5));
                }

                _disposed = true;
            }
        }

        public bool IsDisposed => _disposed;
        #endregion
    }
}
