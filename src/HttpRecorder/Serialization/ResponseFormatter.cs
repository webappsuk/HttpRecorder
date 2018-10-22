using MessagePack;
using MessagePack.Formatters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;

namespace WebApplications.HttpRecorder.Serialization
{
    /// <summary>
    /// Formatter for <see cref="HttpResponseMessage"/>.
    /// </summary>
    /// <seealso cref="MessagePack.Formatters.IMessagePackFormatter{HttpResponseMessage}" />
    internal sealed class ResponseFormatter : IMessagePackFormatter<HttpResponseMessage>
    {
        /// <summary>
        /// The singleton.
        /// </summary>
        public static readonly ResponseFormatter Instance = new ResponseFormatter();

        /// <summary>
        /// Prevents a default instance of the <see cref="ResponseFormatter"/> class from being created.
        /// </summary>
        private ResponseFormatter()
        {
        }

        /// <inheritdoc />
        public int Serialize(ref byte[] bytes, int offset, HttpResponseMessage response, IFormatterResolver formatterResolver)
        {
            if (response == null)
            {
                return MessagePackBinary.WriteNil(ref bytes, offset);
            }

            int startOffset = offset;

            offset += MessagePackBinary.WriteFixedArrayHeaderUnsafe(ref bytes, offset, 5);

            // Version:
            offset += MessagePackBinary.WriteString(ref bytes, offset, response.Version.ToString());

            // Status Code
            offset += MessagePackBinary.WriteUInt16(ref bytes, offset, (ushort)response.StatusCode);

            // ReasonPhrase
            offset += MessagePackBinary.WriteString(ref bytes, offset, response.ReasonPhrase);

            // Headers
            KeyValuePair<string, IEnumerable<string>>[] headers = response.Headers.ToArray();
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

            // NOTE we don't serialize the request, as we substitute with the supplied one in the cassette.

            // Content
            // TODO we need to use all the different formatters here so that we can deserialize to more than just byte[]
            offset += response.Content is null
                ? MessagePackBinary.WriteNil(ref bytes, offset)
                // Note we always buffer before we serialize, so this should never block.
                : MessagePackBinary.WriteBytes(ref bytes, offset,
                    response.Content.ReadAsByteArrayAsync().Result);

            return offset - startOffset;
        }

        /// <inheritdoc />
        public HttpResponseMessage Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            if (MessagePackBinary.IsNil(bytes, offset))
            {
                readSize = 1;
                return null;
            }

            int startOffset = offset;

            int count = MessagePackBinary.ReadArrayHeader(bytes, offset, out readSize);
            offset += readSize;
            if (count != 5) throw new InvalidOperationException("Response format invalid.");

            // Version
            string versionStr = MessagePackBinary.ReadString(bytes, offset, out readSize);
            offset += readSize;
            if (!Version.TryParse(versionStr, out Version version))
                throw new InvalidOperationException($"Response version '{versionStr}' in invalid format!");

            // Status Code
            HttpStatusCode statusCode = (HttpStatusCode)MessagePackBinary.ReadUInt16(bytes, offset, out readSize);
            offset += readSize;

            // ReasonPhrase
            string reasonPhrase = MessagePackBinary.ReadString(bytes, offset, out readSize);
            offset += readSize;

            // Create response so we can start updating it's headers
            HttpResponseMessage response = new HttpResponseMessage(statusCode)
            {
                Version = version,
                ReasonPhrase = reasonPhrase
            };

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

                        response.Headers.Add(key, headerValue);
                    }
                }
            }

            // Content:
            // TODO Deserialize to correct type
#pragma warning disable DF0022 // Marks undisposed objects assinged to a property, originated in an object creation.
            response.Content = new ByteArrayContent(MessagePackBinary.ReadBytes(bytes, offset, out readSize));
#pragma warning restore DF0022 // Marks undisposed objects assinged to a property, originated in an object creation.


            readSize = offset - startOffset;
            return response;
        }
    }
}