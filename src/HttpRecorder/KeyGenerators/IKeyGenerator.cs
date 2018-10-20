using System.Net.Http;

namespace WebApplications.HttpRecorder.KeyGenerators
{
    /// <summary>
    /// Interface for a key generator to uniquely identify keys
    /// </summary>
    public interface IKeyGenerator
    {
        /// <summary>
        /// Gets the unique name of the current generator.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        string Name { get; }

        /// <summary>
        /// Gets a key from the request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>
        /// A unique string that identifies the request, must not be null or empty.
        /// </returns>
        byte[] Generate(HttpRequestMessage request);
    }
}