using BenchmarkDotNet.Running;
using Celestus.Storage.Cache.PerformanceTest;

var summary = BenchmarkRunner.Run<CacheBenchmark>();
