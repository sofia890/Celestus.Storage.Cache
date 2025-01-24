namespace Celestus.Storage.Cache.Test
{
    public class Setup
    {
        [TestInitialize]
        public void Initialize()
        {
            ThreadPool.SetMinThreads(16, 16);
            ThreadPool.SetMaxThreads(32, 32);
        }
    }
}
