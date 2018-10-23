using MessagePack;
using MessagePack.Formatters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;

namespace WebApplications.HttpRecorder.Serialization
{
    /// <summary>
    /// Formatter for <see cref="HttpContent"/>.
    /// </summary>
    /// <seealso cref="IMessagePackFormatter{HttpContent}" />
    internal static class HttpContentFormatter
    {
        /// <summary>
        /// Regex to safely extract charset.
        /// <seealso cref="http://tools.ietf.org/html/rfc7231#section-3.1.1.1">RFC 7231, Sections 3.1.1.1 & 3.1.1.2</seealso>
        /// <seealso cref="http://tools.ietf.org/html/rfc2978#section-5">RFC 2978, Section 5</seealso>
        /// </summary>
        private static readonly Regex _extractCharSet = new Regex(
            @"charset=(\""(?<encoding>[^\"";]+)\""|(?<encoding>[^\"";]+))",
            RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.Compiled |
            RegexOptions.IgnorePatternWhitespace);

        /// <summary>
        /// The known types.
        /// </summary>
        private static readonly IReadOnlyDictionary<Type, byte> _knownTypes
            = new Dictionary<Type, byte>
            {
                {typeof(ByteArrayContent), 0}, // Default
                {typeof(StreamContent), 1},
                {typeof(FormUrlEncodedContent), 2},
                {typeof(StringContent), 3},
                {typeof(MultipartContent), 4},
                {typeof(MultipartFormDataContent), 5}
            };

        /// <summary>
        /// Serializes the specified content.
        /// </summary>
        /// <param name="bytes">The bytes.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="content">The content.</param>
        /// <returns></returns>
        public static int Serialize(ref byte[] bytes, int offset, HttpContent content)
        {
            if (content is null)
                return MessagePackBinary.WriteNil(ref bytes, offset);

            int startOffset = offset;

            offset += MessagePackBinary.WriteFixedArrayHeaderUnsafe(ref bytes, offset, 3);

            // If we know the type, store a byte to indicate which one it is
            if (!_knownTypes.TryGetValue(content.GetType(), out byte type)) type = 0;
            offset += MessagePackBinary.WriteByte(ref bytes, offset, type);

            // Headers
            if (content.Headers == null)
            {
                MessagePackBinary.WriteNil(ref bytes, offset);
                offset++;
            }
            else
            {
                KeyValuePair<string, IEnumerable<string>>[] headers = content.Headers.ToArray();
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
            }

            // All content is written out as a byte array
            offset += MessagePackBinary.WriteBytes(ref bytes, offset,
                content.ReadAsByteArrayAsync().Result);

            return offset - startOffset;
        }

        /// <summary>
        /// Deserializes the content into the specified response.
        /// </summary>
        /// <param name="bytes">The bytes.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="readSize">Size of the read.</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">Content format invalid.</exception>
        public static HttpContent Deserialize(byte[] bytes, int offset, out int readSize)
        {
            if (MessagePackBinary.IsNil(bytes, offset))
            {
                readSize = 1;
                return null;
            }

            int startOffset = offset;

            int count = MessagePackBinary.ReadArrayHeader(bytes, offset, out readSize);
            offset += readSize;
            if (count != 3) throw new InvalidOperationException("Content format invalid.");

            // Type
            byte typeByte = MessagePackBinary.ReadByte(bytes, offset, out readSize);
            offset += readSize;

            // Headers
            Dictionary<string, string[]> headers = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
            if (MessagePackBinary.IsNil(bytes, offset))
            {
                readSize++;
            }
            else
            {
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

                        string[] values = new string[valuesCount];

                        // Note, we can't have an empty array, and if we tried adding one it would be ignored anyway
                        // therefore it's safe to just start looping and call add for each entry.
                        for (int i = 0; i < valuesCount; i++)
                        {
                            string headerValue = MessagePackBinary.ReadString(bytes, offset, out readSize);
                            offset += readSize;

                            values[i] = headerValue;
                        }

                        headers[key] = values;
                    }
                }
            }

            // Get data
            byte[] data = MessagePackBinary.ReadBytes(bytes, offset, out readSize);
            HttpContent content;
            switch (typeByte)
            {
                // StreamContent
                case 1:
#pragma warning disable DF0000 // Marks undisposed anonymous objects from object creations.
                    content = new StreamContent(new MemoryStream(data));
#pragma warning restore DF0000 // Marks undisposed anonymous objects from object creations.
                    break;

                /*
                 *  TODO
                case 2:
                    // See https://github.com/dotnet/corefx/blob/master/src/System.Net.Http/src/System/Net/Http/FormUrlEncodedContent.cs
                    string encoded = Encoding.UTF8.GetString(data).Replace("+", "%20");
                    List<KeyValuePair<string, string>> nvc = new List<KeyValuePair<string, string>>();
                    int start = 0;
                    do
                    {

                    } while ()


                    break;
                */

                case 3:
                    // See https://github.com/dotnet/corefx/blob/master/src/System.Net.Http/src/System/Net/Http/StringContent.cs
                    // Get encoding from headers if possible
                    Encoding encoding;
                    string charSet;
                    if (headers.TryGetValue("content-type", out string[] ctValues) &&
                        ctValues.Length > 0 &&
                        (charSet = ctValues.Select(v => _extractCharSet.Match(v).Groups["encoding"]?.Value)
                            .Last()) != null)
                    {
                        try
                        {
                            encoding = Encoding.GetEncoding(charSet);
                        }
                        catch
                        {
                            encoding = Encoding.UTF8;
                        }
                    }
                    else
                        encoding = Encoding.UTF8;

                    content = new StringContent(encoding.GetString(data), encoding);
                    break;

                // ByteArrayContent
                default:
                    content = new ByteArrayContent(data);
                    break;

            }

            // Replace headers
            content.Headers.Clear();
            foreach (KeyValuePair<string, string[]> header in headers)
            {
                // Sadly some headers fail to deserialize (e.g. Expires -1), so we only try to add here
                foreach (string value in header.Value)
                    content.Headers.TryAddWithoutValidation(header.Key, value);
            }

            readSize = offset - startOffset;

            return content;
        }
    }
}