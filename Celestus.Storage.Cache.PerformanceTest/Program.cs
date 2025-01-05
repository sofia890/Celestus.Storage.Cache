using Celestus.Storage.Cache;
using Celestus.Storage.Cache.PerformanceTest;
using System.Diagnostics;

for (int j = 0; j < 300; j++)
{
    List<double> results = [];

    for (int i = 0; i < 100000; i++)
    {
        var stopwatch2 = new Stopwatch();
        stopwatch2.Start();
        _ = SimpleClass.Calculate((1, 2), out _);
        stopwatch2.Stop();

        var stopwatch = new Stopwatch();
        stopwatch.Start();
        _ = SimpleClass.CalculateCached((1, 2), out _);
        stopwatch.Stop();

        results.Add(stopwatch.Elapsed.TotalMicroseconds - stopwatch2.Elapsed.TotalMicroseconds);
    }

    Console.WriteLine(results[5..].Average());
}
