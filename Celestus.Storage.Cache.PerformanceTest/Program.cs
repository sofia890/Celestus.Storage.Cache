using BenchmarkDotNet.Running;
using Celestus.Storage.Cache.PerformanceTest.Model.Cache;

_ = BenchmarkRunner.Run<CacheBenchmark>();