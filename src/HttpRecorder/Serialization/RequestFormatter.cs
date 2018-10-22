using MessagePack;
using MessagePack.Formatters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace WebApplications.HttpRecorder.Serialization
{
    /// <summary>
    /// Formatter for <see cref="HttpRequestMessage"/>
    /// </summary>
    /// <seealso cref="MessagePack.Formatters.IMessagePackFormatter{HttpRequestMessage}" />
    internal sealed class RequestFormatter : IMessagePackFormatter<HttpRequestMessage>
    {
        /// <summary>
        /// The singleton.
        /// </summary>
        public static readonly RequestFormatter Instance = new RequestFormatter();


        /// <summary>
        /// Prevents a default instance of the <see cref="RequestFormatter"/> class from being created.
        /// </summary>
        private RequestFormatter()
        {
        }

        /// <inheritdoc />
        public int Serialize(ref byte[] bytes, int offset, HttpRequestMessage request,
            IFormatterResolver formatterResolver)
        {
            if (request == null)
            {
                return MessagePackBinary.WriteNil(ref bytes, offset);
            }

            int startOffset = offset;

            offset += MessagePackBinary.WriteFixedArrayHeaderUnsafe(ref bytes, offset, 5);

            // RequestPart.Version:
            offset += MessagePackBinary.WriteString(ref bytes, offset, request.Version.ToString());

            // RequestPart.Uri:
            offset += MessagePackBinary.WriteString(ref bytes, offset, request.RequestUri.ToString());

            // RequestPart.Method:
            offset += MessagePackBinary.WriteString(ref bytes, offset, request.Method.Method);

            // RequestPart.Headers:
            KeyValuePair<string, IEnumerable<string>>[] headers = request.Headers.ToArray();
            offset += MessagePackBinary.WriteMapHeader(ref bytes, offset, headers.Length);
            foreach (KeyValuePair<string, IEnumerable<string>> kvp in headers)
            {
                // See http://www.w3.org/Protocols/rfc2616/rfc2616-sec4.html#sec4.2 Header names are not case-sensitive
                // so we normalize to lower case
                offset += MessagePackBinary.WriteString(ref bytes, offset, kvp.Key.ToLowerInvariant());

                string[] values = kvp.Value.ToArray();
                offset += MessagePackBinary.WriteFixedArrayHeaderUnsafe(ref bytes, offset, values.Length);
                foreach (string v in values)
                    offset += MessagePackBinary.WriteString(ref bytes, offset, v);
            }

            // RequestPart.Content:
            // TODO we need to use all the different formatters here so that we can deserialize to more than just byte[]
            offset += request.Content is null
                ? MessagePackBinary.WriteNil(ref bytes, offset)
                // Note we always buffer before we serialize, so this should never block.
                : MessagePackBinary.WriteBytes(ref bytes, offset,
                    request.Content.ReadAsByteArrayAsync().Result);
            return offset - startOffset;
        }

        /// <inheritdoc />
        public HttpRequestMessage Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            if (MessagePackBinary.IsNil(bytes, offset))
            {
                readSize = 1;
                return null;
            }

            int startOffset = offset;

            int count = MessagePackBinary.ReadArrayHeader(bytes, offset, out readSize);
            offset += readSize;
            if (count != 5) throw new InvalidOperationException("Request format invalid.");

            // RequestPart.Version:
            string versionStr = MessagePackBinary.ReadString(bytes, offset, out readSize);
            offset += readSize;
            if (!Version.TryParse(versionStr, out Version version))
                throw new InvalidOperationException($"Request version '{versionStr}' in invalid format!");

            // RequestPart.UriScheme:
            string uriStr = MessagePackBinary.ReadString(bytes, offset, out readSize);
            offset += readSize;
            if (!Uri.TryCreate(uriStr, UriKind.Absolute, out Uri uri))
                throw new InvalidOperationException($"Request Uri '{uri}' could not be parsed");

            // RequestPart.Method:
            HttpMethod method = HttpMethodFormatter.Instance.Deserialize(bytes, offset, formatterResolver, out readSize);
            offset += readSize;

            // Create request so we can add headers and content
            HttpRequestMessage request = new HttpRequestMessage(method, uri) { Version = version };

            // RequestPart.Headers:
            int headersCount = MessagePackBinary.ReadMapHeader(bytes, offset, out readSize);
            offset += readSize;

            if (headersCount > 0)
            {
                while (headersCount-- > 0)
                {
                    string key = MessagePackBinary.ReadString(bytes, offset, out readSize);
                    offset += readSize;

                    int valuesCount = MessagePackBinary.ReadArrayHeader(bytes, offset, out readSize);
                    offset += readSize;

                    // Note, we can't have an empty array, and if we tried adding one it would be ignored anyway
                    // therefore it's safe to just start looping and call add for each entry.
                    while (valuesCount-- > 0)
                    {
                        string headerValue = MessagePackBinary.ReadString(bytes, offset, out readSize);
                        offset += readSize;

                        request.Headers.Add(key, headerValue);
                    }
                }
            }

            // RequestPart.Content:
            // TODO Deserialize to correct type
#pragma warning disable DF0022 // Marks undisposed objects assinged to a property, originated in an object creation.
            request.Content = new ByteArrayContent(MessagePackBinary.ReadBytes(bytes, offset, out readSize));
#pragma warning restore DF0022 // Marks undisposed objects assinged to a property, originated in an object creation.

            offset += readSize;

            readSize = offset - startOffset;

            return request;
        }
    }
}