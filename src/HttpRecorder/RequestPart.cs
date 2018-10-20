using System.Net.Http;

namespace WebApplications.HttpRecorder
{
    /// <summary>
    /// Part of a <see cref="HttpRequestMessage" />.
    /// </summary>
    public enum RequestPart : ushort
    {
        /// <summary>
        /// The version.
        /// </summary>
        Version = RequestParts.Version,

        /// <summary>
        /// The scheme of the Uri.
        /// </summary>
        UriScheme = RequestParts.UriScheme,

        /// <summary>
        /// The user information of the Uri
        /// </summary>
        UriUserInfo = RequestParts.UriUserInfo,

        /// <summary>
        /// The authority of the Uri.
        /// </summary>
        UriHost = RequestParts.UriHost,

        /// <summary>
        /// The authority of the Uri.
        /// </summary>
        UriPort = RequestParts.UriPort,

        /// <summary>
        /// The URI absolute path.
        /// </summary>
        UriPath = RequestParts.UriPath,

        /// <summary>
        /// The URI query.
        /// </summary>
        UriQuery = RequestParts.UriQuery,

        /// <summary>
        /// The request method.
        /// </summary>
        Method = RequestParts.Method,

        /// <summary>
        /// The request headers.
        /// </summary>
        Headers = RequestParts.Headers,

        /// <summary>
        /// The request content.
        /// </summary>
        Content = RequestParts.Content
    }
}