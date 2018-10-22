using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace WebApplications.HttpRecorder.Stores
{
    /// <summary>
    /// Implements simple memory store with no expiry.
    /// </summary>
    /// <seealso cref="WebApplications.HttpRecorder.Stores.ICassetteStore" />
    public sealed class MemoryStore : ICassetteStore
    {
        /// <summary>
        /// The recordings store, we store task results here to allow for rapid retrieval.
        /// </summary>
        private readonly ConcurrentDictionary<string, Task<byte[]>> _recordings
            = new ConcurrentDictionary<string, Task<byte[]>>();

        /// <inheritdoc />
        public string Name { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MemoryStore"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        public MemoryStore(string name = null)
        {
            if (string.IsNullOrWhiteSpace(name)) name = "Memory Store";
            Name = name;
        }

        /// <inheritdoc />
        public Task<byte[]> GetAsync(string hash, CancellationToken cancellationToken)
            => _recordings.TryGetValue(hash, out Task<byte[]> recording) ? recording : null;

        /// <inheritdoc />
        public Task StoreAsync(string hash, byte[] data)
        {
            // Store task for retrieval performance.
            Task<byte[]> task = Task.FromResult(data);
            _recordings.AddOrUpdate(hash, task, (h, t) => task);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public void Dispose()
        {
        }
    }
}