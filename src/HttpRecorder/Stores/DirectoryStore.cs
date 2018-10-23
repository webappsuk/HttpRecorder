using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace WebApplications.HttpRecorder.Stores
{
    /// <summary>
    /// Stores each data as files in the directory.
    /// </summary>
    /// <seealso cref="WebApplications.HttpRecorder.Stores.ICassetteStore" />
    public sealed class DirectoryStore : ICassetteStore
    {
        /// <summary>
        /// The file name extension.
        /// </summary>
        public const string Extension = ".hrr";

        /// <inheritdoc />
        public string Name { get; }

        // TODO Add Compressed file support...

        /// <summary>
        /// Initializes a new instance of the <see cref="DirectoryStore" /> class.
        /// </summary>
        /// <param name="directoryPath">The directory path.</param>
        /// <exception cref="ArgumentNullException">directoryPath</exception>
        public DirectoryStore(string directoryPath)
        {
            if (string.IsNullOrWhiteSpace(directoryPath))
                throw new ArgumentNullException(nameof(directoryPath));

            Name = directoryPath;

            // Try to create directory if doesn't exist, this deliberately only works to one level
            if (!Directory.Exists(Name)) Directory.CreateDirectory(Name);
        }

        /// <inheritdoc />
        public async Task<byte[]> GetAsync(string hash, CancellationToken cancellationToken = default(CancellationToken))
        {
            string filePath = Path.Combine(Name, hash + Extension);

            if (!File.Exists(filePath)) return null;

            using (FileStream stream = File.OpenRead(filePath))
            using (MemoryStream memoryStream = new MemoryStream())
            {
                await stream.CopyToAsync(memoryStream, 81920, cancellationToken);
                return memoryStream.ToArray();
            }
        }

        /// <inheritdoc />
        public async Task StoreAsync(string hash, byte[] data)
        {
            string filePath = Path.Combine(Name, hash + Extension);

            using (FileStream stream = File.Open(filePath, FileMode.Create))
                await stream.WriteAsync(data, 0, data.Length);
        }

        /// <inheritdoc />
        public void Dispose()
        {
        }
    }
}