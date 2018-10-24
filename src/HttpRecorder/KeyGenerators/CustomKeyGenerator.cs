using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace WebApplications.HttpRecorder.KeyGenerators
{
    public delegate Task<byte[]> GenerateDelegate(HttpRequestMessage request, CancellationToken cancellationToken = default(CancellationToken));

    /// <summary>
    /// Allows for creation of <see cref="IKeyGenerator"/> using a <see cref="GenerateDelegate">delegate</see>.
    /// </summary>
    /// <seealso cref="KeyGenerator" />
    public class CustomKeyGenerator : KeyGenerator
    {
        /// <summary>
        /// The delegate.
        /// </summary>
        private readonly GenerateDelegate _delegate;

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomKeyGenerator" /> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="generator">The generator.</param>
        /// <exception cref="ArgumentOutOfRangeException">generator</exception>
        public CustomKeyGenerator(string name, GenerateDelegate generator) : base(name)
            => _delegate = generator ?? throw new ArgumentOutOfRangeException(nameof(generator));

        /// <inheritdoc />
        public override Task<byte[]> Generate(HttpRequestMessage request, CancellationToken cancellationToken) =>
            _delegate(request, cancellationToken);
    }
}