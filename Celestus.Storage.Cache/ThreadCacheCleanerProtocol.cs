namespace Celestus.Storage.Cache
{
    internal enum CleanerProtocol
    {
        TrackEntryInd,
        RegisterRemovalCallbackInd,
        Registercollection,
        ResetInd,
        Stop
    }

    internal record Signal(CleanerProtocol SignalId);

    internal record TrackEntryInd<KeyType>(KeyType Key, CacheEntry Entry) : Signal(CleanerProtocol.TrackEntryInd);

    internal record RegisterRemovalCallbackInd<KeyType>(WeakReference<Func<List<KeyType>, bool>> Callback) : Signal(CleanerProtocol.RegisterRemovalCallbackInd);
    internal record RegistercollectionInd<KeyType>(WeakReference<IEnumerable<KeyValuePair<KeyType, CacheEntry>>> collection) : Signal(CleanerProtocol.Registercollection);

    internal record ResetInd(long CleanupIntervalInTicks) : Signal(CleanerProtocol.ResetInd);
}
