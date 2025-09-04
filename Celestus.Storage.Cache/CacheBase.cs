using Celestus.Io;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Celestus.Storage.Cache
{
    public class CacheIoException(string message) : IOException(message);
    public class CacheLoadException(string message) : CacheIoException(message);
    public class CacheSaveException(string message) : CacheIoException(message);
    public class NoPersistentPathException(string message) : CacheIoException(message);

    public abstract class CacheBase<KeyType> : IDisposable
        where KeyType : notnull
    {
        public const int NO_TIMEOUT = -1;

        public string Key { get; init; }
        internal abstract CacheCleanerBase<KeyType> Cleaner { get; }
        public abstract bool IsDisposed { get; }
        internal abstract Dictionary<KeyType, CacheEntry> Storage { get; set; }


        [MemberNotNullWhen(true, nameof(PersistentStorageLocation))]
        public bool Persistent { get => PersistentStorageLocation != null; }
        public Uri? PersistentStorageLocation { get; init; }

        public CacheBase(string key, bool persistent = false, string persistentStoragePath = "")
        {
            Key = key;

            if (persistent && persistentStoragePath.Length > 0)
            {
                PersistentStorageLocation = new(persistentStoragePath);
            }
            else if (persistent)
            {
                PersistentStorageLocation = GetDefaultPersistentPath(key);
            }
            else
            {
                PersistentStorageLocation = null;
            }

            HandlePersistentInitialization();
        }

        public abstract void Dispose();

        public abstract DataType? Get<DataType>(string key);

        public abstract void Set<DataType>(string key, DataType value, TimeSpan? duration = null);

        public abstract bool TryRemove(KeyType[] key);

        private static Uri GetDefaultPersistentPath(string key)
        {
            string commonAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            var appPath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;

            var assemblyName = Assembly.GetExecutingAssembly().GetName().Name;

            Uri filePath;

            if (appPath == null)
            {
                filePath = new($"{Directory.GetCurrentDirectory()}/{assemblyName}/{key}.json");
            }
            else
            {
                var file = new FileInfo(appPath);

                var appName = file.Name;

                filePath = new($"{commonAppDataPath}/{appName}/{assemblyName}/{key}.json");

                if (!CanWrite.Test(filePath))
                {
                    filePath = new Uri($"{file.DirectoryName}/{assemblyName}/{key}.json");
                }
            }

            if (!CanWrite.Test(filePath))
            {
                throw new NoPersistentPathException("Could not find any writeable path for application.");
            }

            return filePath;
        }

        public void HandlePersistentInitialization()
        {
            if (Persistent)
            {
                var file = new FileInfo(PersistentStorageLocation.AbsolutePath);

                if (file.Exists &&
                    file.Length > 0 &&
                    !TryLoadFromFile(PersistentStorageLocation))
                {
                    throw new CacheLoadException($"Could not load cache for key '{Key}'.");
                }
            }
        }

        public void HandlePersistentFinalization()
        {
            if (Persistent)
            {
                if (!File.Exists(PersistentStorageLocation.AbsolutePath))
                {
                    _ = Directory.CreateDirectory(PersistentStorageLocation.AbsolutePath);
                }

                if (!TrySaveToFile(PersistentStorageLocation))
                {
                    throw new CacheSaveException($"Could not save cache for key '{Key}'.");
                }
            }
        }
        public abstract bool TrySaveToFile(Uri path);

        public abstract bool TryLoadFromFile(Uri path);
    }
}
