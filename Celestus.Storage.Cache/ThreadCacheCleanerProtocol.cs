﻿namespace Celestus.Storage.Cache
{
    internal enum CleanerProtocol
    {
        EntryAccessedInd,
        TrackEntryInd,
        RegisterRemovalCallbackInd,
        ResetInd
    }

    internal record Signal(CleanerProtocol SignalId);

    internal record EntryAccessedInd<KeyType>(KeyType Key) : Signal(CleanerProtocol.EntryAccessedInd);

    internal record TrackEntryInd<KeyType>(KeyType Key, CacheEntry Entry) : Signal(CleanerProtocol.TrackEntryInd);

    internal record RegisterRemovalCallbackInd<KeyType>(Func<List<KeyType>, bool> Callback) : Signal(CleanerProtocol.RegisterRemovalCallbackInd);

    internal record ResetInd(long CleanupIntervalInTicks) : Signal(CleanerProtocol.ResetInd);
}
