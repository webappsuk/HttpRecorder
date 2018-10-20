using System.Net.Http;

namespace WebApplications.HttpRecorder.KeyGenerators
{
    /// <summary>
    /// Base class for <see cref="IKeyGenerator"/> implementations.
    /// </summary>
    /// <seealso cref="IKeyGenerator" />
    public abstract class KeyGeneratorBase : IKeyGenerator
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="KeyGeneratorBase"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        protected KeyGeneratorBase(string name)
        {
            Name = name;
        }

        /// <inheritdoc />
        public string Name { get; }

        /// <inheritdoc />
        public abstract byte[] Generate(HttpRequestMessage request);
    }
}