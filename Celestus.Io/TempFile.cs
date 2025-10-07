namespace Celestus.Io
{
    public class TempFile : IDisposable
    {
        private bool _disposed;

        public FileInfo Info { get; init; } = new(Path.GetTempFileName());

        #region IDisposable
        /// <summary>
        /// Releases all resources used by the <see cref="FilePath"/> and deletes the temporary file.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="FilePath"/> and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing && Info.Exists)
                {
                    Info.Delete();
                }

                _disposed = true;
            }
        }

        /// <summary>
        /// Finalizer that ensures the temporary file is deleted even if Dispose is not called.
        /// </summary>
        ~TempFile()
        {
            Dispose(false);
        }
        #endregion
    }
}
