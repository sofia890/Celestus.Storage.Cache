namespace Celestus.Storage.Cache
{
    internal enum CleanerProtocol
    {
        TrackEntryInd,
        RegisterCacheInd,
        ResetInd,
        Stop
    }

    internal record Signal(CleanerProtocol SignalId);

    internal record RegisterCacheInd<KeyType>(WeakReference<CacheBase<KeyType>> Cache) : Signal(CleanerProtocol.RegisterCacheInd)
        where KeyType : notnull;

    internal record ResetInd(long CleanupIntervalInTicks) : Signal(CleanerProtocol.ResetInd);

    internal record StopInd() : Signal(CleanerProtocol.Stop);
}
