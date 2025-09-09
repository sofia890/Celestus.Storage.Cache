namespace Celestus.Storage.Cache.Test.Model
{
    internal static class CacheConstants
    {
        public static TimeSpan VeryShortDuration { get => TimeSpan.FromMilliseconds(1); }
        public static TimeSpan ShortDuration { get => TimeSpan.FromMilliseconds(4); }
        public static TimeSpan LongDuration { get => TimeSpan.FromMilliseconds(30); }
        public static TimeSpan VeryLongDuration { get => TimeSpan.FromSeconds(60); }
        public static TimeSpan TimingDuration { get => TimeSpan.FromMilliseconds(400); }
        public static TimeSpan ZeroDuration { get => TimeSpan.Zero; }
        public static int TimingIterations { get => 10; }
        public static TimeSpan TimingIterationInterval { get => TimeSpan.FromMilliseconds(100); }
    }
}
