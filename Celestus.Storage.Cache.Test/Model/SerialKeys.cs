namespace Celestus.Storage.Cache.Test.Model
{
    public class SerialKeys
    {
        long index = 0;

        public string Current()
        {
            return $"Key_{index}";
        }

        public string Next()
        {
            index++;

            return Current();
        }
    }
}
