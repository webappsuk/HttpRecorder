using System;

namespace WebApplications.HttpRecorder.Internal
{
    /// <summary>
    /// Used to hold serialized request/response pairs.
    /// </summary>
    internal class Recording
    {
        public readonly string Hash;

        public readonly string KeyGeneratorName;

        public readonly DateTime RecordedUtc;

        public readonly int DurationMs;

        internal readonly byte[] ResponseData;

        /// <summary>
        /// </summary>
        internal readonly byte[] RequestData;

        /// <summary>
        /// Initializes a new instance of the <see cref="Recording"/> class.
        /// </summary>
        /// <param name="hash">The hash.</param>
        /// <param name="keyGeneratorName">Name of the key generator.</param>
        /// <param name="recordedUtc">The recorded UTC.</param>
        /// <param name="durationMs">The duration ms.</param>
        /// <param name="responseData">The response data.</param>
        /// <param name="requestData">The request data.</param>
        internal Recording(
            string hash,
            string keyGeneratorName,
            DateTime recordedUtc,
            int durationMs,
            byte[] responseData,
            byte[] requestData)
        {
            Hash = hash;
            KeyGeneratorName = keyGeneratorName;
            RecordedUtc = recordedUtc;
            DurationMs = durationMs;
            ResponseData = responseData;
            RequestData = requestData;
        }
    }
}
