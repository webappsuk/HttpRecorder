using MessagePack;
using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using WebApplications.HttpRecorder.Serialization;

namespace WebApplications.HttpRecorder.KeyGenerators
{
    /// <summary>
    /// Generates keys based on the <see cref="RequestParts"/> enumeration.
    /// </summary>
    /// <seealso cref="KeyGenerator" />
    public class RequestPartsKeyGenerator : KeyGenerator
    {
        /// <summary>
        /// The key generators by enumeration.
        /// </summary>
        private static readonly ConcurrentDictionary<RequestParts, RequestPartsKeyGenerator> _keyGenerators = new ConcurrentDictionary<RequestParts, RequestPartsKeyGenerator>();

        /// <summary>
        /// The prefix.
        /// </summary>
        private static readonly string _prefix = $"{BuiltinPrefix}RP";

        /// <summary>
        /// Generates a key using all the request.
        /// </summary>
        public static readonly RequestPartsKeyGenerator All;

        /// <summary>
        /// Initializes the <see cref="RequestPartsKeyGenerator"/> class.
        /// </summary>
        static RequestPartsKeyGenerator()
        {
            All = FullRequestKeyGenerator.Instance;
            _keyGenerators.GetOrAdd(RequestParts.All, All);
        }

        /// <summary>
        /// The parts to use.
        /// </summary>
        public readonly RequestParts Parts;

        /// <summary>
        /// Creates a <see cref="RequestPartsKeyGenerator" />.
        /// </summary>
        /// <param name="parts">The parts.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException">name</exception>
        internal static KeyGenerator Get(RequestParts parts)
        {
            if (parts == RequestParts.Default)
                parts = RequestParts.All;
            return _keyGenerators.GetOrAdd(parts, p => new RequestPartsKeyGenerator(p, $"{_prefix}{(ushort)p}"));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestPartsKeyGenerator" /> class.
        /// </summary>
        /// <param name="parts">The parts.</param>
        /// <param name="name">The name.</param>
        internal RequestPartsKeyGenerator(RequestParts parts, string name) : base(name, true)
            => Parts = parts;

        /// <inheritdoc />
        public override async Task<byte[]> Generate(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // If we rely on content then ensure the content is fully loaded before serialization.
            if (Parts.HasFlag(RequestParts.Content) && !(request.Content is null))
                await request.Content.LoadIntoBufferAsync();

            // IMPORTANT: Cassette assumes this key generator uses the full standard serializer!
            return MessagePackSerializer.Serialize(request, RecorderResolver.Get(Parts));
        }
    }
}