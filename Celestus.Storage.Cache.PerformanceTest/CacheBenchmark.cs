using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace Celestus.Storage.Cache.PerformanceTest
{
    [SimpleJob(RuntimeMoniker.Net90)]
    public class CacheBenchmark
    {
        [Benchmark]
        public void Native()
        {
            _ = SimpleClass.Calculate((100, 1), out _);
        }

        [Benchmark]
        public void Cached()
        {
            _ = SimpleClass.CalculateCached((100, 1), out _);
        }

        [Benchmark]
        public void NativeHeavy()
        {
            _ = SimpleClass.Calculate((100, 200000), out _);
        }

        [Benchmark]
        public void CachedHeavy()
        {
            _ = SimpleClass.CalculateCached((100, 200000), out _);
        }
    }
}
