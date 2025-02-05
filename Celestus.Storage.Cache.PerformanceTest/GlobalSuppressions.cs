// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(
    "Performance",
    "CA1822:Mark members as static",
    Justification = "BenchmarkDotNet does not support static methods.",
    Scope = "type",
    Target = "~T:Celestus.Storage.Cache.PerformanceTest.Model.Cache.CacheBenchmark")]

[assembly: SuppressMessage(
    "Performance",
    "CA1822:Mark members as static",
    Justification = "BenchmarkDotNet does not support static methods.",
    Scope = "type",
    Target = "~T:Celestus.Storage.Cache.PerformanceTest.Model.Signaling.SignalingBenchmark")]
