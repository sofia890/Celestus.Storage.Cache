namespace Celestus.Storage.Cache
{
    internal enum CleanerProtocol
    {
        EntryAccessedInd,
        TrackEntryInd,
        RegisterRemovalCallbackInd,
        ResetInd,
        Stop
    }

    internal record Signal(CleanerProtocol SignalId);

    internal record EntryAccessedInd<KeyType>(KeyType Key, long TimeInTicks) : Signal(CleanerProtocol.EntryAccessedInd);

    internal record TrackEntryInd<KeyType>(KeyType Key, CacheEntry Entry) : Signal(CleanerProtocol.TrackEntryInd);

    internal record RegisterRemovalCallbackInd<KeyType>(WeakReference<Func<List<KeyType>, bool>> Callback) : Signal(CleanerProtocol.RegisterRemovalCallbackInd);

    internal record ResetInd(long CleanupIntervalInTicks) : Signal(CleanerProtocol.ResetInd);
}
