namespace Celestus.Io
{
    public static class CanWrite
    {
        public static bool Test(FileInfo fileInfo)
        {
            try
            {
                using var fileStream = fileInfo.Open(FileMode.Open);

                return fileStream.CanWrite;
            }
            catch
            {
                return false;
            }
            
        }
    }
}
