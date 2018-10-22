using MessagePack;
using MessagePack.Formatters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace WebApplications.HttpRecorder.Serialization
{
    internal class RequestPartsFormatter : IMessagePackFormatter<HttpRequestMessage>
    {
        public readonly RequestParts Parts;
        private readonly IReadOnlyList<RequestPart> _parts;

        public RequestPartsFormatter(RequestParts requestParts)
        {
            Parts = requestParts;
            _parts = requestParts.GetParts().ToArray();
        }

        /// <inheritdoc />
        public int Serialize(ref byte[] bytes, int offset, HttpRequestMessage request, IFormatterResolver formatterResolver)
        {
            if (request is null)
                return MessagePackBinary.WriteNil(ref bytes, offset);

            int startOffset = offset;

            Uri uri = request.RequestUri;
            offset += MessagePackBinary.WriteFixedArrayHeaderUnsafe(ref bytes, offset, _parts.Count + 1);

            // Add the parts into the key output, to further mangle the hash and prevent collisions.
            offset += MessagePackBinary.WriteUInt16(ref bytes, offset, (ushort)Parts);

            foreach (RequestPart part in _parts)
            {
                switch (part)
                {
                    case RequestPart.Version:
                        offset += MessagePackBinary.WriteString(ref bytes, offset, request.Version.ToString());
                        break;
                    case RequestPart.UriScheme:
                        offset += MessagePackBinary.WriteString(ref bytes, offset, uri.Scheme);
                        break;
                    case RequestPart.UriUserInfo:
                        offset += MessagePackBinary.WriteString(ref bytes, offset, uri.UserInfo);
                        break;
                    case RequestPart.UriHost:
                        offset += MessagePackBinary.WriteString(ref bytes, offset, uri.Host);
                        break;
                    case RequestPart.UriPort:
                        int port = uri.Port;
                        // Note -1 is returned for a protocol with no default port and no port specified, this
                        // should never really happen in practice.  Also port numbers should never exceed 65535.
                        // For these edge case we treat as the default port and we store 0, which is a special port
                        // That we should never explicitly use in a Http request.
                        if (port < 1 || port > ushort.MaxValue)
                            port = 0;
                        offset += MessagePackBinary.WriteUInt16(ref bytes, offset, (ushort)port);
                        break;
                    case RequestPart.UriPath:
                        // Strip the preceding '/' from path.
                        offset += MessagePackBinary.WriteString(ref bytes, offset, uri.AbsolutePath.Substring(1));
                        break;
                    case RequestPart.UriQuery:
                        // Strip the preceding '?' from path.
                        offset += MessagePackBinary.WriteString(ref bytes, offset, uri.Query.Substring(1));
                        break;
                    case RequestPart.Method:
                        offset += MessagePackBinary.WriteString(ref bytes, offset, request.Method.Method);
                        break;
                    case RequestPart.Headers:
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
                        break;
                    case RequestPart.Content:
                        // As we're using for key gen we can safely ignore the type
                        offset += request.Content is null
                            ? MessagePackBinary.WriteNil(ref bytes, offset)
                            // Note we always buffer before we serialize, so this should never block.
                            : MessagePackBinary.WriteBytes(ref bytes, offset,
                                request.Content.ReadAsByteArrayAsync().Result);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            return offset - startOffset;
        }

        /// <inheritdoc />
        public HttpRequestMessage Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            // We don't support deserialization as we don't have all the parts needed to create a HttpRequestMessage and we
            // ultimately hash this anyway!
            throw new InvalidOperationException("Cannot deserialize a HttpRequest key using a RequestParts formatter!");
        }
    }
}
