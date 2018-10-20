using MessagePack;
using MessagePack.Formatters;
using System;
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
        public int Serialize(ref byte[] bytes, int offset, HttpResponseMessage value, IFormatterResolver formatterResolver)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public HttpResponseMessage Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            throw new NotImplementedException();
        }
    }
}