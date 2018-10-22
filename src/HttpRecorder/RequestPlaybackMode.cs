using System.Net.Http;

namespace WebApplications.HttpRecorder
{
    /// <summary>
    /// Determines how the recorder should handle requests returned in a <see cref="HttpResponseMessage.RequestMessage"/>.
    /// </summary>
    public enum RequestPlaybackMode
    {
        /// <summary>
        /// The recorder will substitute a recorded request into the response if available; otherwise, it will use the provided request.
        /// </summary>
        Auto,
        /// <summary>
        /// The recorder will always use the provided request in the response.
        /// </summary>
        IgnoreRecorded,
        /// <summary>
        /// The recorder will error if it finds a recorded response without a recorded request.
        /// request.
        /// </summary>
        UseRecorded
    }
}