using BenchmarkDotNet.Running;
using Celestus.Storage.Cache.PerformanceTest;
using Celestus.Storage.Cache.PerformanceTest.Model.Signalling;

//var summary = BenchmarkRunner.Run<CacheBenchmark>();
var summary = BenchmarkRunner.Run<BufferBlockBenchmark>();
