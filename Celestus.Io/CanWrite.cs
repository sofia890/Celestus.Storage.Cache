namespace Celestus.Io
{
    public static class CanWrite
    {
        public static bool Test(Uri path)
        {
            try
            {
                File.WriteAllText(path.AbsolutePath, string.Empty);
                File.Delete(path.AbsolutePath);

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
