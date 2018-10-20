using MessagePack;
using System;
using System.Net.Http;
using WebApplications.HttpRecorder.Serialization;

namespace WebApplications.HttpRecorder.KeyGenerators
{
    /// <summary>
    /// Generates keys based on the <see cref="RequestParts"/> enumeration.
    /// </summary>
    /// <seealso cref="KeyGeneratorBase" />
    public class RequestPartsKeyGenerator : KeyGeneratorBase
    {
        /// <summary>
        /// The prefix.
        /// </summary>
        internal const string Prefix = "RP";

        /// <summary>
        /// The parts to use.
        /// </summary>
        public readonly RequestParts Parts;

        /// <summary>
        /// Creates a <see cref="RequestPartsKeyGenerator"/>.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException">name</exception>
        internal static IKeyGenerator Create(string name)
        {
            string shortStr = name.Substring(3);
            RequestParts parts;
            if (!ushort.TryParse(shortStr, out ushort s) ||
                (parts = (RequestParts)s) > RequestParts.All)
                throw new ArgumentOutOfRangeException(nameof(name), name.Length,
                    $"The built-in name {name} doesn't map to the {nameof(RequestParts)} enum.");

            // Always map default to all to ensure we have some bits set.
            return parts == RequestParts.Default || parts == RequestParts.All
                ? FullRequestKeyGenerator.Instance
                : new RequestPartsKeyGenerator(parts, name);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestPartsKeyGenerator" /> class.
        /// </summary>
        /// <param name="parts">The parts.</param>
        /// <param name="name">The name.</param>
        protected RequestPartsKeyGenerator(RequestParts parts, string name) : base(name)
            => Parts = parts;

        /// <inheritdoc />
        public override byte[] Generate(HttpRequestMessage request)
            => MessagePackSerializer.Serialize(request, RecorderResolver.Get(Parts));
    }
}