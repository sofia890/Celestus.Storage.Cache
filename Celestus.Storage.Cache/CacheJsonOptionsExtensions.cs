using Celestus.Storage.Cache;
using System.Runtime.CompilerServices;
using System.Text.Json;

public static class CacheJsonOptionsExtensions
{
    private sealed class Holder { public BlockedEntryBehavior Behavior; }
    private static readonly ConditionalWeakTable<JsonSerializerOptions, Holder> _table = new();

    public static void SetBlockedEntryBehavior(this JsonSerializerOptions options, BlockedEntryBehavior behavior)
    {
        _table.GetValue(options, _ => new Holder()).Behavior = behavior;
    }

    public static BlockedEntryBehavior GetBlockedEntryBehavior(this JsonSerializerOptions options,
        BlockedEntryBehavior @default = BlockedEntryBehavior.Throw)
    {
        return _table.TryGetValue(options, out var h) ? h.Behavior : @default;
    }
}