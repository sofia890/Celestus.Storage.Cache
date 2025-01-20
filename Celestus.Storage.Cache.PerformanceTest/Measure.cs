using System.Numerics;

namespace Celestus.Storage.Cache.PerformanceTest
{
    public static class Measure
    {
        public static DataType Average<DataType>(IEnumerable<DataType> samples, int nrOfValuesToSkipAtEachExtreme)
            where DataType : IDivisionOperators<DataType, DataType, DataType>, IAdditionOperators<DataType, DataType, DataType>, INumber<DataType>, new()
        {
            var nrOfSamples = samples.Count() - nrOfValuesToSkipAtEachExtreme * 2;
            var usedSamples = samples.Order().Skip(nrOfValuesToSkipAtEachExtreme).Take(nrOfSamples);
            DataType sum = new();

            foreach (var sample in usedSamples)
            {
                sum += sample;
            }

            return sum / DataType.Parse($"{nrOfSamples}", null);

        }

        public static IEnumerable<DataType> Run<DataType>(Func<DataType> method, int iterationsPerThread, int nrOfThreads = 1, TimeSpan timeout = default)
        {
            using ManualResetEvent startEvent = new(false);

            List<DataType> Run()
            {
                List<DataType> results = [];

                startEvent.WaitOne();

                for (int i = 0; i < iterationsPerThread; i++)
                {
                    results.Add(method());
                }

                return results;
            }

            Task<List<DataType>>[] tasks = [.. Enumerable.Range(0, nrOfThreads).Select(x => Task.Run(Run))];
            startEvent.Set();

            if (timeout == default)
            {
                Task.WaitAll(tasks);
            }
            else
            {
                Task.WaitAll(tasks, timeout);
            }

            return tasks.SelectMany(x => x.Result);
        }
    }
}
