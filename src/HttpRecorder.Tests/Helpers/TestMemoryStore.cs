using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WebApplications.HttpRecorder.Stores;

namespace WebApplications.HttpRecorder.Tests.Helpers
{
    /// <summary>
    /// The test memory store allows tests to access store requests.  Note it is not thread safe!
    /// </summary>
    /// <seealso cref="IDictionary{TKey, TValue}" />
    /// <seealso cref="ICassetteStore" />
    public sealed class TestMemoryStore : IDictionary<string, byte[]>, ICassetteStore
    {
        /// <summary>
        /// The recordings store, we store task results here to allow for rapid retrieval.
        /// </summary>
        private readonly IDictionary<string, byte[]> _recordings
            = new Dictionary<string, byte[]>();

        /// <inheritdoc />
        public string Name { get; }

        /// <summary>
        /// Gets a value indicating how many times <see cref="GetAsync"/> has been called.
        /// </summary>
        /// <value>
        /// The store asynchronous count.
        /// </value>
        public int GetAsyncCount { get; private set; }

        /// <summary>
        /// Gets a value indicating how many times <see cref="StoreAsync"/> has been called.
        /// </summary>
        /// <value>
        /// The store asynchronous count.
        /// </value>
        public int StoreAsyncCount { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MemoryStore"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        public TestMemoryStore(string name = null)
        {
            if (string.IsNullOrWhiteSpace(name)) name = "Test Memory Store";
            Name = name;
        }

        /// <inheritdoc />
        public Task<byte[]> GetAsync(string hash, CancellationToken cancellationToken)
        {
            GetAsyncCount++;
            return Task.FromResult(_recordings.TryGetValue(hash, out byte[] recording) ? recording : null);
        }

        /// <inheritdoc />
        public Task StoreAsync(string hash, byte[] data)
        {
            // Store task for retrieval performance.
            StoreAsyncCount++;
            _recordings[hash] = data;
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public void Dispose()
        {
        }

        /// <inheritdoc />
        public IEnumerator<KeyValuePair<string, byte[]>> GetEnumerator() => _recordings.GetEnumerator();

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_recordings).GetEnumerator();

        /// <inheritdoc />
        public void Add(KeyValuePair<string, byte[]> item) => _recordings.Add(item);

        /// <inheritdoc />
        public void Clear() => _recordings.Clear();

        /// <inheritdoc />
        public bool Contains(KeyValuePair<string, byte[]> item) => _recordings.Contains(item);

        /// <inheritdoc />
        public void CopyTo(KeyValuePair<string, byte[]>[] array, int arrayIndex) => _recordings.CopyTo(array, arrayIndex);

        /// <inheritdoc />
        public bool Remove(KeyValuePair<string, byte[]> item) => _recordings.Remove(item);

        /// <inheritdoc />
        public int Count => _recordings.Count;

        /// <inheritdoc />
        public bool IsReadOnly => _recordings.IsReadOnly;

        /// <inheritdoc />
        public void Add(string key, byte[] value) => _recordings.Add(key, value);

        /// <inheritdoc />
        public bool ContainsKey(string key) => _recordings.ContainsKey(key);

        /// <inheritdoc />
        public bool Remove(string key) => _recordings.Remove(key);

        /// <inheritdoc />
        public bool TryGetValue(string key, out byte[] value) => _recordings.TryGetValue(key, out value);

        /// <inheritdoc />
        public byte[] this[string key]
        {
            get => _recordings[key];
            set => _recordings[key] = value;
        }

        /// <inheritdoc />
        public ICollection<string> Keys => _recordings.Keys;

        /// <inheritdoc />
        public ICollection<byte[]> Values => _recordings.Values;
    }
}