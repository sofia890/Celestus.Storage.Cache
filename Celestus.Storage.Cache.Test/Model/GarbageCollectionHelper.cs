using System.Diagnostics;

namespace Celestus.Storage.Cache.Test.Model
{
    internal class GarbageCollectionHelper<Value>
    {
        public delegate object OperationDelegate(out Value value);

        public static Value? ActAndCollect(OperationDelegate operation, out bool wasReleased, TimeSpan timeout)
        {
            List<WeakReference> weakReference = [];
            List<object> strongReference = [];

            Value? value = default;

            weakReference.Add(Weak.CreateReference(() =>
            {
                object reference = operation(out var result);
                value = result;

                strongReference.Add(reference);

                return reference;
            }));

            strongReference.Clear();

            Stopwatch stopwatch = Stopwatch.StartNew();

            while (!weakReference.All(weak => !weak.IsAlive) && stopwatch.Elapsed < timeout)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

            wasReleased = weakReference.All(weak => !weak.IsAlive);

            return value;
        }
    }
}
