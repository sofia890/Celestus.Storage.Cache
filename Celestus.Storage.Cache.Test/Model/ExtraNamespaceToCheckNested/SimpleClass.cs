﻿using Celestus.Storage.Cache.Attributes;

namespace Celestus.Storage.Cache.Test.Model.ExtraNamespaceToCheckNested
{
    public partial class SimpleClass
    {
        readonly int _justAnotherVariable = 0;

        public const int CALCULATE_TIMEOUT = 5;
        public const int CALCULATE_WITTH_SLEEP_TIMEOUT = 10;
        public const int CALCULATION_SLEEP = 5;
        public const int CALCULATE_WITTH_SLEEP_DURATION = 100;

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

        [Cache(timeoutInMilliseconds: CALCULATE_WITTH_SLEEP_TIMEOUT, durationInMs: CALCULATE_WITTH_SLEEP_DURATION)]
        public int SleepBeforeCalculation((int a, int b) inData, out int c)
        {
            ThreadHelper.SpinWait(CALCULATION_SLEEP);

            return Calculate(inData, out c);
        }
    }
}