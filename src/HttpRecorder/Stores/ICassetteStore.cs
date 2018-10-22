using System;
using System.Threading;
using System.Threading.Tasks;

namespace WebApplications.HttpRecorder.Stores
{
    /// <summary>
    /// Allows for storage and retrieval of recording data.
    /// </summary>
    public interface ICassetteStore : IDisposable
    {
        /// <summary>
        /// Gets the name of the cassette.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        string Name { get; }

        /// <summary>
        /// Gets the data with the specified hash from the store asynchronously.
        /// </summary>
        /// <param name="hash">The hash.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// An awaitable task that returns the data if any;
        /// otherwise <see langword="null" />.
        /// </returns>
        Task<byte[]> GetAsync(string hash, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Stores the data asynchronously.
        /// </summary>
        /// <param name="hash">The hash.</param>
        /// <param name="data">The data.</param>
        /// <returns>
        /// An awaitable task.
        /// </returns>
        Task StoreAsync(string hash, byte[] data);
    }
}
