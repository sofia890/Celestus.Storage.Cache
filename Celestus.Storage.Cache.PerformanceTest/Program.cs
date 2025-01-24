using Celestus.Storage.Cache;
using Celestus.Storage.Cache.Test.Model;

//var summary = BenchmarkRunner.Run<CacheBenchmark>();



//
// Arrange
//
const int CLEAN_INTERVAL_IN_MS = 1;
var cache = new ThreadCache(new ThreadCacheCleaner<string>(cleanupIntervalInMs: CLEAN_INTERVAL_IN_MS));

static byte[] CreateElement()
{
    const int ELEMENT_SIZE = 10;
    return new byte[ELEMENT_SIZE];
}

var keys = new SerialKeys();
var firstKey = keys.Next();
_ = cache.TrySet(firstKey, CreateElement(), TimeSpan.FromDays(1));

const int N_ITERATIONS = 100000;

for (int i = 0; i < N_ITERATIONS; i++)
{
    _ = cache.TrySet(keys.Next(), CreateElement(), TimeSpan.FromMilliseconds(CLEAN_INTERVAL_IN_MS));
}

Thread.Sleep(CLEAN_INTERVAL_IN_MS * 20);