using BenchmarkDotNet.Running;
using Celestus.Storage.Cache.PerformanceTest.Model.Signaling;

var summary = BenchmarkRunner.Run<SignalingBenchmark>();
