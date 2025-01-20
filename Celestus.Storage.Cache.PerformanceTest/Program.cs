using Celestus.Storage.Cache.PerformanceTest;
using System.Diagnostics;

static TimeSpan Test()
{
    var stopwatchNormalCall = new Stopwatch();
    stopwatchNormalCall.Start();
    _ = SimpleClass.Calculate((1, 2), out _);
    stopwatchNormalCall.Stop();

    var stopwatchCachedCall = new Stopwatch();
    stopwatchCachedCall.Start();
    _ = SimpleClass.CalculateCached((1, 2), out _);
    stopwatchCachedCall.Stop();

    return TimeSpan.FromTicks(stopwatchCachedCall.ElapsedTicks - stopwatchNormalCall.ElapsedTicks);
}

var measurements = Measure.Run(Test, iterationsPerThread: 1000, nrOfThreads: 4).Select(x => x.Ticks);

var average = Measure.Average(measurements, nrOfValuesToSkipAtEachExtreme: 100);
TimeSpan averageTimeSpan = TimeSpan.FromTicks(average);

Console.WriteLine($"Average extra processing time for cached is {averageTimeSpan} ({averageTimeSpan.TotalMicroseconds} µs)");
