namespace Celestus.Storage.Cache.Test.Model
{
    public class SerialKeys
    {
        long index = 0;

        public string Next()
        {
            return $"Key_{index++}";
        }

        public string Current()
        {
            return $"Key_{index}";
        }
    }
}
