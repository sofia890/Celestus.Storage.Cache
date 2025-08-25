namespace Celestus.Storage.Cache
{
    internal enum CleanerProtocol
    {
        TrackEntryInd,
        RegisterRemovalCallbackInd,
        RegisterCollection,
        ResetInd,
        Stop
    }

    internal record Signal(CleanerProtocol SignalId);

    internal record RegisterRemovalCallbackInd<KeyType>(WeakReference<Func<List<KeyType>, bool>> Callback) : Signal(CleanerProtocol.RegisterRemovalCallbackInd);
    internal record RegisterCollectionInd<KeyType>(WeakReference<IEnumerable<KeyValuePair<KeyType, CacheEntry>>> collection) : Signal(CleanerProtocol.RegisterCollection);

    internal record ResetInd(long CleanupIntervalInTicks) : Signal(CleanerProtocol.ResetInd);
}
