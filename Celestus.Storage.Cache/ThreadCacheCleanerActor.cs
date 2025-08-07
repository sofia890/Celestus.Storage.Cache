using System.Text.Json;
using System.Threading.Channels;

namespace Celestus.Storage.Cache
{
    internal class ThreadCacheCleanerActor<KeyType> : IDisposable
    {
        List<(KeyType key, CacheEntry entry)> _entries = [];
        long _cleanupIntervalInTicks;
        long _nextCleanupOpportunityInTicks = 0;
        Func<List<KeyType>, bool> _removalCallback = (keys) => false;
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
                    if (!_disposed)
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

                        case CleanerProtocol.EntryAccessedInd when rawSignal is EntryAccessedInd<KeyType> payload:
                            Prune(payload.TimeInTicks);
                            break;

                        case CleanerProtocol.TrackEntryInd when rawSignal is TrackEntryInd<KeyType> payload:
                            Prune(DateTime.UtcNow.Ticks);

                            _entries.Add((payload.Key, payload.Entry));
                            break;

                        case CleanerProtocol.RegisterRemovalCallbackInd when rawSignal is RegisterRemovalCallbackInd<KeyType> payload:
                            _removalCallback = payload.Callback;
                            break;

                        case CleanerProtocol.ResetInd when rawSignal is ResetInd payload:
                            _entries.Clear();
                            _cleanupIntervalInTicks = payload.CleanupIntervalInTicks;
                            _nextCleanupOpportunityInTicks = 0;
                            break;
                    }
                }
            }
        }

        private void Prune(long timeInTicks)
        {
            if (_disposed || _entries.Count == 0 || _nextCleanupOpportunityInTicks > timeInTicks)
            {
                return;
            }

            List<KeyType> expiredKeys = [];
            List<(KeyType key, CacheEntry entry)> remainingElements = new(_entries.Count);

            for (int i = 0; i < _entries.Count; i++)
            {
                var element = _entries[i];

                if (CacheCleaner<KeyType>.ExpiredCriteria(element.entry, timeInTicks))
                {
                    expiredKeys.Add(element.key);
                }
                else
                {
                    remainingElements.Add(element);
                }
            }

            _entries = remainingElements;

            if (expiredKeys.Count > 0 && !_disposed)
            {
                _ = _removalCallback(expiredKeys);
            }

            _nextCleanupOpportunityInTicks = timeInTicks + _cleanupIntervalInTicks;
        }

        public void ReadSettings(ref Utf8JsonReader reader)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }

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
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }

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
                    CleanerPort.Writer.Complete();

                    _signalHandlerTask.Wait(TimeSpan.FromSeconds(5));

                    _entries.Clear();
                }

                _disposed = true;
            }
        }

        ~ThreadCacheCleanerActor()
        {
            Dispose(false);
        }
        #endregion
    }
}
