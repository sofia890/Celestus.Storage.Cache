namespace Celestus.Storage.Cache
{
    internal enum CleanerProtocol
    {
        TrackEntryInd,
        RegisterCacheInd,
        UnregisterCacheInd,
        ResetInd,
        Stop
    }

    internal record Signal(CleanerProtocol SignalId);

    internal record RegisterCacheInd<CacheIdType, CacheKeyType>(WeakReference<CacheBase<CacheIdType, CacheKeyType>> Cache) : Signal(CleanerProtocol.RegisterCacheInd)
        where CacheIdType : notnull
        where CacheKeyType : notnull;

    internal record UnregisterCacheInd() : Signal(CleanerProtocol.UnregisterCacheInd);

    internal record ResetInd(long CleanupIntervalInTicks) : Signal(CleanerProtocol.ResetInd);

    internal record StopInd() : Signal(CleanerProtocol.Stop);
}
