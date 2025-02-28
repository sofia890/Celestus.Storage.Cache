using System.Text.Json;
using System.Threading.Channels;

namespace Celestus.Storage.Cache
{
    internal class ThreadCacheCleanerActor<KeyType>
    {
        List<(KeyType key, CacheEntry entry)> _entries = [];
        long _cleanupIntervalInTicks;
        long _nextCleanupOpportunityInTicks = 0;
        Func<List<KeyType>, bool> _removalCallback = (keys) => false;
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

            Task.Run(HandleSignals);
        }

        private Task NewTimeoutTask()
        {
            return Task.Delay(TimeSpan.FromTicks(_cleanupIntervalInTicks));
        }

        private async void HandleSignals()
        {
            Task<Signal>? signalTask = null;

            var reader = CleanerPort.Reader;

            while (!reader.Completion.IsCompleted)
            {
                if (signalTask == null)
                {
                    signalTask = reader.ReadAsync().AsTask();
                }
                else if (await Task.WhenAny(signalTask, NewTimeoutTask()) != signalTask)
                {
                    Prune();
                }
                else if (signalTask.IsCompletedSuccessfully)
                {
                    Signal rawSignal = signalTask.Result;
                    signalTask = null;

                    switch (rawSignal.SignalId)
                    {
                        default:
                        case CleanerProtocol.EntryAccessedInd:
                            Prune();
                            break;

                        case CleanerProtocol.TrackEntryInd when rawSignal is TrackEntryInd<KeyType> payload:
                            Prune();

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

        private void Prune()
        {
            var currentTimeInTicks = DateTime.UtcNow.Ticks;

            if (_entries.Count > 0 && _nextCleanupOpportunityInTicks <= currentTimeInTicks)
            {
                List<KeyType> expiredKeys = [];
                List<(KeyType key, CacheEntry entry)> remainingElements = new(_entries.Count);

                for (int i = 0; i < _entries.Count; i++)
                {
                    var element = _entries[i];

                    if (CacheCleaner<string>.ExpiredCriteria(element.entry, currentTimeInTicks))
                    {
                        expiredKeys.Add(element.key);
                    }
                    else
                    {
                        remainingElements.Add(element);
                    }
                }

                _entries = remainingElements;

                if (expiredKeys.Count > 0)
                {
                    _ = _removalCallback(expiredKeys);
                }

                _nextCleanupOpportunityInTicks = currentTimeInTicks + _cleanupIntervalInTicks;
            }
        }

        public void ReadSettings(ref Utf8JsonReader reader)
        {
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
                throw new JsonException($"Missing parameter {nameof(_cleanupIntervalInTicks)}.");
            }
        }

        public void WriteSettings(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName(nameof(_cleanupIntervalInTicks));
            writer.WriteNumberValue(_cleanupIntervalInTicks);
            writer.WriteEndObject();
        }
    }
}
