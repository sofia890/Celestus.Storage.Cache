namespace Celestus.Storage.Cache.Test.Model
{
    internal static class ElementHelper
    {
        public static byte[] CreateSmallArray()
        {
            const int ELEMENT_SIZE = 10;
            return new byte[ELEMENT_SIZE];
        }
    }
}
