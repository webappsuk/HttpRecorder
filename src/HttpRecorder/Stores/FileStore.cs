using System;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;

namespace WebApplications.HttpRecorder.Stores
{
    public sealed class FileStore : ICassetteStore
    {
        /// <summary>
        /// The file name extension.
        /// </summary>
        public const string DefaultExtension = ".hrc";

        private ZipArchive _archive;
        private SemaphoreSlim _lock;

        /// <summary>
        /// Gets the file path of the cassette.
        /// </summary>
        /// <value>
        /// The file path.
        /// </value>
        public string Name { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FileStore" /> class.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <exception cref="ArgumentNullException">filePath</exception>
#pragma warning disable DF0021 // Marks undisposed objects assinged to a field, originated from method invocation.
#pragma warning disable DF0020 // Marks undisposed objects assinged to a field, originated in an object creation.
        public FileStore(string filePath)
        {
            Name = filePath ?? throw new ArgumentNullException(nameof(filePath));
            _archive = ZipFile.Open(filePath, ZipArchiveMode.Update);
            _lock = new SemaphoreSlim(1);
        }
#pragma warning restore DF0020 // Marks undisposed objects assinged to a field, originated in an object creation.
#pragma warning restore DF0021 // Marks undisposed objects assinged to a field, originated from method invocation.

        /// <inheritdoc />
        public async Task<byte[]> GetAsync(string hash, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (string.IsNullOrWhiteSpace(hash))
                throw new ArgumentNullException(nameof(hash));

            await _lock.WaitAsync(cancellationToken);
            try
            {
                ZipArchiveEntry entry = _archive.GetEntry(hash);
                if (entry is null) return null;
                using (Stream stream = entry.Open())
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    await stream.CopyToAsync(memoryStream, 81920, cancellationToken);
                    return memoryStream.ToArray();
                }
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <inheritdoc />
        public async Task StoreAsync(string hash, byte[] data)
        {
            if (data is null)
                throw new ArgumentNullException(nameof(data));

            await _lock.WaitAsync();
            try
            {
                // Remove existing entry if any
                ZipArchiveEntry entry = _archive.GetEntry(hash);
                entry?.Delete();

                // Create new entry
                entry = _archive.CreateEntry(hash);
                using (Stream stream = entry.Open())
                    await stream.WriteAsync(data, 0, data.Length);
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Interlocked.Exchange(ref _archive, null)?.Dispose();
            Interlocked.Exchange(ref _lock, null)?.Dispose();
        }
    }
}