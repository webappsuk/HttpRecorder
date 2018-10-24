using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace WebApplications.HttpRecorder.KeyGenerators
{
    /// <summary>
    /// Base class for Key Generator implementations.
    /// </summary>
    public abstract class KeyGenerator
    {
        /// <summary>
        /// The reserved prefix for builtin key generators.
        /// </summary>
        internal const char BuiltinPrefix = '_';

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyGenerator"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="isBuiltin">if set to <c>true</c> is a built-in class an so can start with .</param>
        internal KeyGenerator(string name, bool isBuiltin)
        {
            if (name is null)
                throw new ArgumentNullException(nameof(name));
            if (name.Length < 3)
                throw new ArgumentOutOfRangeException(nameof(name), name,
                    "Key generator name is too short, must be at least 2 characters.");
            if (!isBuiltin && name[0] == BuiltinPrefix)
                throw new ArgumentOutOfRangeException(nameof(name), name,
                    $"The '{BuiltinPrefix}' is reserved for built-in key generators.");

            Name = name;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyGenerator"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        protected KeyGenerator(string name)
        : this(name, false)
        {
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name { get; }

        /// <summary>
        /// Generates a unique key from the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A key.</returns>
        public abstract Task<byte[]> Generate(HttpRequestMessage request, CancellationToken cancellationToken);
    }
}