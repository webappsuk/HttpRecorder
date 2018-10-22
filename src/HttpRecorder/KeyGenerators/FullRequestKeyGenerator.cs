using MessagePack;
using System.Net.Http;
using WebApplications.HttpRecorder.Serialization;

namespace WebApplications.HttpRecorder.KeyGenerators
{
    /// <summary>
    /// Uses all of the request for key generation.
    /// </summary>
    /// <seealso cref="WebApplications.HttpRecorder.KeyGenerators.RequestPartsKeyGenerator" />
    public sealed class FullRequestKeyGenerator : RequestPartsKeyGenerator
    {
        /// <summary>
        /// The prefix.
        /// </summary>
        internal const string Prefix = "FL";

        /// <summary>
        /// The singleton.
        /// </summary>
        public static readonly IKeyGenerator Instance = new FullRequestKeyGenerator();

        /// <summary>
        /// Prevents a default instance of the <see cref="FullRequestKeyGenerator"/> class from being created.
        /// </summary>
        private FullRequestKeyGenerator() : base(RequestParts.All, $"{KeyGeneratorResolver.BuiltinPrefixChar}{Prefix}")
        {
        }

        /// <inheritdoc />
        public override byte[] Generate(HttpRequestMessage request)
            // IMPORTANT: Cassette assumes this key generator uses the full standard serializer!
            => MessagePackSerializer.Serialize(request, RecorderResolver.Instance);
    }
}