using System.Diagnostics.CodeAnalysis;

namespace Celestus.Exceptions
{
    public static class Condition
    {
        public static void ThrowIf<ExceptionType>([DoesNotReturnIf(true)]  bool condition, string message = "", object[]? parameters = null)
            where ExceptionType : Exception
        {
            if (condition)
            {
                object[] usedParameters = [message];

                if (parameters != null)
                {
                    usedParameters = parameters;
                }

                throw (ExceptionType)Activator.CreateInstance(typeof(ExceptionType), usedParameters)!;
            }
        }
    }
}