
using System.Text.Json;
using System.Threading.Channels;

namespace Celestus.Storage.Cache
{
    public class ThreadCacheCleaner<KeyType>(int cleanupIntervalInMs) : CacheCleanerBase<KeyType>()
    {
        private enum CleanerProtocol
        {
            EntryAccessedInd,
            TrackEntryInd,
            RegisterRemovalCallbackInd,
            ResetInd
        }

        private record Signal(CleanerProtocol SignalId);

        private record EntryAccessedInd(KeyType Key) : Signal(CleanerProtocol.EntryAccessedInd);

        private record TrackEntryInd(KeyType Key, CacheEntry Entry) : Signal(CleanerProtocol.TrackEntryInd);

        private record RegisterRemovalCallbackInd(Func<List<KeyType>, bool> Callback) : Signal(CleanerProtocol.RegisterRemovalCallbackInd);

        private record ResetInd(long CleanupIntervalInTicks) : Signal(CleanerProtocol.ResetInd);

        private class CleanerActor
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

            public CleanerActor(int cleanupIntervalInMs)
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

                            case CleanerProtocol.TrackEntryInd when rawSignal is TrackEntryInd payload:
                                Prune();

                                _entries.Add((payload.Key, payload.Entry));
                                break;

                            case CleanerProtocol.RegisterRemovalCallbackInd when rawSignal is RegisterRemovalCallbackInd payload:
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

        const int DEFAULT_INTERVAL_IN_MS = 60000;

        readonly CleanerActor _server = new(cleanupIntervalInMs);

        public ThreadCacheCleaner() : this(DEFAULT_INTERVAL_IN_MS)
        {

        }

        ~ThreadCacheCleaner()
        {
            _server.CleanerPort.Writer.Complete();
        }

        public override void TrackEntry(ref CacheEntry entry, KeyType key)
        {
            _server.CleanerPort.Writer.WriteAsync(new TrackEntryInd(key, entry));
        }

        public override void EntryAccessed(ref CacheEntry entry, KeyType key)
        {
            _server.CleanerPort.Writer.WriteAsync(new EntryAccessedInd(key));
        }

        public override void RegisterRemovalCallback(Func<List<KeyType>, bool> callback)
        {
            _server.CleanerPort.Writer.WriteAsync(new RegisterRemovalCallbackInd(callback));
        }

        public override void ReadSettings(ref Utf8JsonReader reader, JsonSerializerOptions options)
        {
            _server.ReadSettings(ref reader);
        }

        public override void WriteSettings(Utf8JsonWriter writer, JsonSerializerOptions options)
        {
            _server.WriteSettings(writer);
        }
    }
}
