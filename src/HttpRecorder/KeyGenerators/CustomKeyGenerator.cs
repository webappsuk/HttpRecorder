using System;
using System.Net.Http;

namespace WebApplications.HttpRecorder.KeyGenerators
{
    public delegate byte[] GenerateDelegate(HttpRequestMessage request);

    /// <summary>
    /// Allows for creation of <see cref="IKeyGenerator"/> using a <see cref="GenerateDelegate">delegate</see>.
    /// </summary>
    /// <seealso cref="WebApplications.HttpRecorder.KeyGenerators.KeyGeneratorBase" />
    public class CustomKeyGenerator : KeyGeneratorBase
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
        public override byte[] Generate(HttpRequestMessage request) => _delegate(request);
    }
}