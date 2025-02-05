using Celestus.Storage.Cache.Attributes;

namespace Celestus.Storage.Cache.PerformanceTest
{
    public partial class SimpleClass
    {
        public const int CALCULATE_TIMEOUT = 100;

        [Cache(timeoutInMilliseconds: CALCULATE_TIMEOUT)]
        public static int Calculate((int a, int b) inData, out int c)
        {
            c = inData.a - inData.b;

            for (int i = 0; i < inData.a * inData.b; i++)
            {
                c += i;
            }

            return inData.a + inData.b;
        }
    }
}
