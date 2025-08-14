namespace Celestus.Storage.Cache.Test.Model
{
    internal class GarbageCollectionHelper<Value>
    {
        public delegate object OperationDelegate(out Value value);

        public Value? ActAndCollect(OperationDelegate operation, out bool wasReleased)
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

            GC.Collect();
            GC.WaitForPendingFinalizers();

            wasReleased = weakReference.All(weak => !weak.IsAlive);

            return value;
        }
    }
}
