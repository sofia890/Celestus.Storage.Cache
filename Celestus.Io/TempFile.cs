namespace Celestus.Io
{
    public class TempFile : IDisposable
    {
        private bool _disposed;

        public Uri Uri { get; init; } = new Uri(Path.GetTempFileName());

        /// <summary>
        /// Initializes a new instance of the TempFile class.
        /// </summary>
        /// <param name="createFile">If true, creates the temporary file immediately; otherwise, the file is created when first written to.</param>
        /// <param name="content">The initial content to write to the file if createFile is true. If null, an empty string is written.</param>
        public TempFile(bool createFile = false, string? content = null)
        {
            if (createFile)
            {
                WriteAllText(content ?? string.Empty);
            }
        }

        public FileInfo ToFileInfo()
        {
            return new FileInfo(Uri.AbsolutePath);
        }

        public void WriteAllText(string value)
        {
             File.WriteAllText(Uri.AbsolutePath, value);
        }

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
                if (disposing && File.Exists(Uri.AbsolutePath))
                {
                    File.Delete(Uri.AbsolutePath);
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
