using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Celestus.Storage.Cache
{
    public static class JsonSerializerOptionsExtensions
    {
        private sealed class MetadataHolder
        {
            public BlockedEntryBehavior Behavior;
            public CacheTypeRegister? Register;
        }

        private static readonly ConditionalWeakTable<JsonSerializerOptions, MetadataHolder> _metadata = new();

        public static void SetBlockedEntryBehavior(this JsonSerializerOptions options, BlockedEntryBehavior behavior)
        {
            if (_metadata.TryGetValue(options, out var holder))
            {
                holder.Behavior = behavior;
            }
            else
            {
                _metadata.Add(options, new MetadataHolder { Behavior = behavior });
            }
        }

        public static BlockedEntryBehavior GetBlockedEntryBehavior(this JsonSerializerOptions options)
        {
            return _metadata.TryGetValue(options, out var holder) ? holder.Behavior : BlockedEntryBehavior.Throw;
        }

        public static void SetCacheTypeRegister(this JsonSerializerOptions options, CacheTypeRegister register)
        {
            if (register is null)
            {
                return; // Ignore null assignment for safety.
            }
            if (_metadata.TryGetValue(options, out var holder))
            {
                holder.Register = register;
            }
            else
            {
                _metadata.Add(options, new MetadataHolder { Register = register, Behavior = BlockedEntryBehavior.Throw });
            }
        }

        public static CacheTypeRegister GetCacheTypeRegister(this JsonSerializerOptions options)
        {
            return _metadata.TryGetValue(options, out var holder) ? holder.Register ?? new() : new();
        }
    }
}
