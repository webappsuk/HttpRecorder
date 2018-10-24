using MessagePack;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using WebApplications.HttpRecorder.Serialization;

namespace WebApplications.HttpRecorder.KeyGenerators
{
    /// <summary>
    /// Uses all of the request for key generation.
    /// </summary>
    /// <seealso cref="WebApplications.HttpRecorder.KeyGenerators.RequestPartsKeyGenerator" />
    internal sealed class FullRequestKeyGenerator : RequestPartsKeyGenerator
    {
        /// <summary>
        /// The prefix.
        /// </summary>
        private static readonly string _prefix = $"{BuiltinPrefix}FL";

        /// <summary>
        /// The singleton.
        /// </summary>
        public static readonly FullRequestKeyGenerator Instance = new FullRequestKeyGenerator();

        /// <summary>
        /// Prevents a default instance of the <see cref="FullRequestKeyGenerator"/> class from being created.
        /// </summary>
        private FullRequestKeyGenerator() : base(RequestParts.All, _prefix)
        {
        }

        /// <inheritdoc />
        public override async Task<byte[]> Generate(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Ensure the content is fully loaded before serialization.
            if (!(request.Content is null))
                await request.Content.LoadIntoBufferAsync();

            // IMPORTANT: Cassette assumes this key generator uses the full standard serializer!
            return MessagePackSerializer.Serialize(request, RecorderResolver.Instance);
        }
    }
}