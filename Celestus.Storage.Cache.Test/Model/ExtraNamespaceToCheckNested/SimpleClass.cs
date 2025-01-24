using Celestus.Storage.Cache.Attributes;

namespace Celestus.Storage.Cache.Test.Model.ExtraNamespaceToCheckNested
{
    public partial class SimpleClass
    {
        readonly int _justAnotherVariable = 0;

        public const int CALCULATE_TIMEOUT = 25;
        public const int CALCULATE_WITTH_SLEEP_TIMEOUT = 50;
        public const int CALCULATION_SLEEP = 25;

        [Cache(timeoutInMilliseconds: ExampleReferenceClass.Value + 5, key: "keyTest" + "55")]
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
            System.Threading.Thread.Sleep(CALCULATION_SLEEP);

            return Calculate(inData, out c);
        }
    }
}