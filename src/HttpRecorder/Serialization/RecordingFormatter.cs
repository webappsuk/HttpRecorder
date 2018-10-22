using MessagePack;
using MessagePack.Formatters;
using System;
using System.Net.Http;
using WebApplications.HttpRecorder.Internal;

namespace WebApplications.HttpRecorder.Serialization
{
    /// <summary>
    /// Formatter for <see cref="HttpRequestMessage"/>
    /// </summary>
    /// <seealso cref="MessagePack.Formatters.IMessagePackFormatter{Recording}" />
    internal sealed class RecordingFormatter : IMessagePackFormatter<Recording>
    {
        /// <summary>
        /// The singleton.
        /// </summary>
        public static readonly RecordingFormatter Instance = new RecordingFormatter();


        /// <summary>
        /// Prevents a default instance of the <see cref="RequestFormatter"/> class from being created.
        /// </summary>
        private RecordingFormatter()
        {
        }

        /// <inheritdoc />
        public int Serialize(ref byte[] bytes, int offset, Recording recording,
            IFormatterResolver formatterResolver)
        {
            if (recording is null)
                return MessagePackBinary.WriteNil(ref bytes, offset);

            int startOffset = offset;

            offset += MessagePackBinary.WriteFixedArrayHeaderUnsafe(ref bytes, offset, 6);
            offset += MessagePackBinary.WriteString(ref bytes, offset, recording.Hash);
            offset += MessagePackBinary.WriteString(ref bytes, offset, recording.KeyGeneratorName);
            offset += MessagePackBinary.WriteDateTime(ref bytes, offset, recording.RecordedUtc);
            offset += MessagePackBinary.WriteInt32(ref bytes, offset, recording.DurationMs);

            offset += MessagePackBinary.WriteBytes(ref bytes, offset, recording.ResponseData);

            if (recording.RequestData is null)
                offset += MessagePackBinary.WriteNil(ref bytes, offset);
            else
                offset += MessagePackBinary.WriteBytes(ref bytes, offset, recording.RequestData);
            return offset - startOffset;
        }

        /// <inheritdoc />
        public Recording Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            if (MessagePackBinary.IsNil(bytes, offset))
            {
                readSize = 1;
                return null;
            }

            int startOffset = offset;

            int count = MessagePackBinary.ReadArrayHeader(bytes, offset, out readSize);
            offset += readSize;
            // NOTE: We can use this to distinguish between future versions of a Recording.
            if (count != 6) throw new InvalidOperationException("Request format invalid.");

            string hash = MessagePackBinary.ReadString(bytes, offset, out readSize);
            offset += readSize;

            string keyGeneratorName = MessagePackBinary.ReadString(bytes, offset, out readSize);
            offset += readSize;

            DateTime recordedUtc = MessagePackBinary.ReadDateTime(bytes, offset, out readSize);
            offset += readSize;

            int durationMs = MessagePackBinary.ReadInt32(bytes, offset, out readSize);
            offset += readSize;

            byte[] responseData = MessagePackBinary.ReadBytes(bytes, offset, out readSize);
            offset += readSize;

            byte[] requestData;
            if (MessagePackBinary.IsNil(bytes, offset))
            {
                requestData = null;
                offset++;
            }
            else
            {
                requestData = MessagePackBinary.ReadBytes(bytes, offset, out readSize);
                offset += readSize;
            }
            readSize = offset - startOffset;

            return new Recording(
                hash,
                keyGeneratorName,
                recordedUtc,
                durationMs,
                responseData,
                requestData);
        }
    }
}