namespace WebApplications.HttpRecorder
{
    public enum RecordMode
    {
        /// <summary>
        /// Records messages to a cassette if a request match is not found;
        /// otherwise is will use the cassette to replay.
        /// </summary>
        Auto,

        /// <summary>
        /// Plays back from the cassette if a request match is found;
        /// otherwise it will error.
        /// </summary>
        Playback,

        /// <summary>
        /// Records to the cassette any new requests it sees, otherwise
        /// it will pass through silently.
        /// </summary>
        Record,

        /// <summary>
        /// Records to the cassette, updating any matching request.
        /// </summary>
        Overwrite,

        /// <summary>
        /// No recording will occur.
        /// </summary>
        None,
    }
}