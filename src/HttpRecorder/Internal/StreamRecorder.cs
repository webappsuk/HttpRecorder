using System;
using System.IO;

namespace WebApplications.HttpRecorder.Internal
{
    /// <summary>
    /// Instruments a stream, wrapping it to allow recording of data passed through it.
    /// </summary>
    /// <seealso cref="Stream" />
    internal class StreamRecorder : Stream
    {
        /// <summary>
        /// The inner stream.
        /// </summary>
        private readonly Stream _innerStream;

        /// <summary>
        /// The record stream.
        /// </summary>
        private readonly Stream _recordStream;

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamRecorder"/> class.
        /// </summary>
        /// <param name="innerStream">The inner stream.</param>
        /// <param name="recordStream">The record stream.</param>
        /// <exception cref="ArgumentNullException">
        /// innerStream
        /// or
        /// recordStream
        /// </exception>
        /// <exception cref="ArgumentException">recordStream is not writable</exception>
        public StreamRecorder(Stream innerStream, Stream recordStream)
        {
            _innerStream = innerStream ?? throw new ArgumentNullException(nameof(innerStream));
            _recordStream = recordStream ?? throw new ArgumentNullException(nameof(recordStream));
            if (!recordStream.CanWrite)
                throw new ArgumentException("recordStream is not writable");
        }

        /// <inheritdoc />
        public override void Flush() => _innerStream.Flush();

        /// <inheritdoc />
        public override long Seek(long offset, SeekOrigin origin) => _innerStream.Seek(offset, origin);

        /// <inheritdoc />
        public override void SetLength(long value) => _innerStream.SetLength(value);

        /// <inheritdoc />
        public override int Read(byte[] buffer, int offset, int count)
        {
            int bytesRead = _innerStream.Read(buffer, offset, count);

            if (bytesRead != 0)
                _recordStream.Write(buffer, offset, bytesRead);

            return bytesRead;
        }

        /// <inheritdoc />
        public override void Write(byte[] buffer, int offset, int count)
        {
            _innerStream.Write(buffer, offset, count);
            _recordStream.Write(buffer, offset, count);
        }

        /// <inheritdoc />
        public override bool CanRead => _innerStream.CanRead;

        /// <inheritdoc />
        public override bool CanSeek => _innerStream.CanSeek;

        /// <inheritdoc />
        public override bool CanWrite => _innerStream.CanWrite;

        /// <inheritdoc />
        public override long Length => _innerStream.Length;

        /// <inheritdoc />
        public override long Position
        {
            get => _innerStream.Position;
            set => _innerStream.Position = value;
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing) => _innerStream.Dispose();
    }
}