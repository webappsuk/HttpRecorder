using MessagePack;
using MessagePack.Formatters;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;

namespace WebApplications.HttpRecorder.Serialization
{
    /// <summary>
    /// Formatter for <see cref="HttpMethod"/>.
    /// </summary>
    /// <seealso cref="IMessagePackFormatter{HttpMethod}" />
    internal sealed class HttpMethodFormatter : IMessagePackFormatter<HttpMethod>
    {
        /// <summary>
        /// The singleton.
        /// </summary>
        public static readonly HttpMethodFormatter Instance = new HttpMethodFormatter();

        /// <summary>
        /// Cache of values.
        /// </summary>
        private static readonly ConcurrentDictionary<string, HttpMethod> _cachedHttpMethods =
            new ConcurrentDictionary<string, HttpMethod>(new Dictionary<string, HttpMethod>
            {
                {"GET", HttpMethod.Get},
                {"PUT", HttpMethod.Put},
                {"POST", HttpMethod.Post},
                {"DELETE", HttpMethod.Delete},
                {"HEAD", HttpMethod.Head},
                {"OPTIONS", HttpMethod.Options},
                {"TRACE", HttpMethod.Trace}
            }, StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Prevents a default instance of the <see cref="HttpMethodFormatter" /> class from being created.
        /// </summary>
        private HttpMethodFormatter()
        {
        }

        /// <inheritdoc />
        public int Serialize(ref byte[] bytes, int offset, HttpMethod method, IFormatterResolver formatterResolver)
            => method is null
                ? MessagePackBinary.WriteNil(ref bytes, offset)
                : MessagePackBinary.WriteString(ref bytes, offset, method.Method);

        /// <inheritdoc />
        public HttpMethod Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            if (MessagePackBinary.IsNil(bytes, offset))
            {
                readSize = 1;
                return null;
            }

            string name = MessagePackBinary.ReadString(bytes, offset, out readSize);
            if (string.IsNullOrEmpty(name)) return null;

            name = name.ToUpperInvariant();
            return _cachedHttpMethods.GetOrAdd(name, n => new HttpMethod(name));
        }
    }
}