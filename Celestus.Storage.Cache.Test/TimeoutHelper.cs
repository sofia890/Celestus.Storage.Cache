namespace Celestus.Storage.Cache.Test
{
    internal class TimeoutHelper
    {
        public static string GetKeyThatTakesLongToHash() => new('a', 100000);
        public static (bool threadsSucceeded, bool primaryResult) RunInParallel<PrimaryDataType, SecondaryDataType>(
            (int nrOfThreads,
             Func<PrimaryDataType> init,
             Func<PrimaryDataType, int, bool> operation) primary,
            (int nrOfThreads,
             Func<SecondaryDataType> init,
             Func<SecondaryDataType, int, bool> operation) secondary,
            int iterationsPerThread,
            int timeoutInMs)
        {
            Func<bool> ThreadMethodFactory<DataType>(
                Func<DataType> init,
                Func<DataType, int, bool> operation)
            {
                return () =>
                {
                    var data = init();

                    var succeeded = false;

                    for (int i = 0; i < iterationsPerThread; i++)
                    {
                        succeeded |= operation(data, i);
                    }

                    return succeeded;
                };
            }

            var primaryThreads = Enumerable.Range(0, primary.nrOfThreads)
                                           .Select(x => Task.Run(ThreadMethodFactory(primary.init, primary.operation)))
                                           .ToArray();
            var secondaryThreads = Enumerable.Range(0, secondary.nrOfThreads)
                                             .Select(x => Task.Run(ThreadMethodFactory(secondary.init, secondary.operation)))
                                             .ToArray();

            return (Task.WaitAll(secondaryThreads, timeoutInMs) && Task.WaitAll(primaryThreads, timeoutInMs),
                    primaryThreads.Any(x => x.Result));
        }
    }
}
