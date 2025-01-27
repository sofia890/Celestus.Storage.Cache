using BenchmarkDotNet.Running;
using Celestus.Storage.Cache.PerformanceTest;
using Celestus.Storage.Cache.PerformanceTest.Model.Signaling;

//var summary = BenchmarkRunner.Run<CacheBenchmark>();
var summary = BenchmarkRunner.Run<BufferBlockBenchmark>();
