using Celestus.Storage.Cache.Attributes;

namespace Celestus.Storage.Cache.Test
{
    public record ExampleRecord(int A, string B, decimal C);

    namespace ExtraNamespaceToCheckNested
    {
        public partial class SimpleClass
        {
            readonly int _justAnotherVariable = 0;

            public const int CALCULATE_TIMEOUT = 100;
            public const int CALCULATE_WITTH_SLEEP_TIMEOUT = 300;
            public const int CALCULATION_SLEEP = 250;

            [Cache(timeoutInMilliseconds: CALCULATE_TIMEOUT)]
            public int Calculate((int a, int b) inData, out int c)
            {
                return _justAnotherVariable + CalculateStatic(inData, out c);
            }

            [Cache(timeoutInMilliseconds: CALCULATE_TIMEOUT)]
            public static int CalculateStatic((int a, int b) inData, out int c)
            {
                c = inData.a - inData.b;

                return inData.a + inData.b;
            }

            [Cache(timeoutInMilliseconds: CALCULATE_WITTH_SLEEP_TIMEOUT)]
            public int SleepBeforeCalculation((int a, int b) inData, out int c)
            {
                Thread.Sleep(CALCULATION_SLEEP);

                return Calculate(inData, out c);
            }
        }
    }
}
