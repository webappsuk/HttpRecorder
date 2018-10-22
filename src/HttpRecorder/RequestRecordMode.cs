namespace WebApplications.HttpRecorder
{
    /// <summary>
    /// Determines how the recorder should handle requests being changed by a message handler.
    /// </summary>
    public enum RequestRecordMode
    {
        /// <summary>
        /// The recorder will not detect request changes, or record the request.  When playing back a response the request
        /// that is passed in will be attached to the response that is returned.
        /// </summary>
        Ignore,
        /// <summary>
        /// The recorder will detect request changes, and only record the request if a change is detected.
        /// When playing back a response, the request that is passed in will be attached to the response that is returned
        /// if a request is not present in the recording; otherwise the recorded request will be attached to the response.
        /// </summary>
        RecordIfChanged,
        /// <summary>
        /// The recorder will always record the request and attach it to the response on playback, ignoring the passed in
        /// request.
        /// </summary>
        AlwaysRecord
    }
}