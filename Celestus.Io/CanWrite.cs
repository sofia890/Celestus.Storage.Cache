namespace Celestus.Io
{
    public static class CanWrite
    {
        public static bool Test(FileInfo file)
        {
            try
            {
                File.WriteAllText(file.FullName, string.Empty);
                File.Delete(file.FullName);

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
