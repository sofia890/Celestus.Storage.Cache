namespace Celestus.Storage.Cache.Test.Model
{
    internal static class Weak
    {
        public static WeakReference CreateReference<T>(Func<T> factory)
        {
            return new WeakReference(factory());
        }
    }
}
