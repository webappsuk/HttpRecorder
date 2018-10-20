using System;
using System.Net.Http;

namespace WebApplications.HttpRecorder
{
    /// <summary>
    /// Combination of <see cref="RequestPart">parts</see> of a <see cref="HttpRequestMessage" />.
    /// </summary>
    [Flags]
    public enum RequestParts : ushort
    {
        /// <summary>
        /// The default, is the same as specifying <see cref="All"/>.
        /// </summary>
        /// <remarks>This makes it impossible to specify a value with no bits, set.</remarks>
        Default = 0,

        /// <summary>
        /// The version.
        /// </summary>
        Version = 1 << 0,

        /// <summary>
        /// The scheme of the Uri.
        /// </summary>
        /// <remarks>
        /// For example, uses 'https' from 'https://u:p@localhost:1234/a/b/c?a=1'.
        /// </remarks>
        UriScheme = 1 << 1,

        /// <summary>
        /// The user information of the Uri
        /// </summary>
        /// <remarks>
        /// For example, uses 'u:p' from 'https://u:p@localhost:1234/a/b/c?a=1'.
        /// </remarks>
        UriUserInfo = 1 << 2,

        /// <summary>
        /// The authority of the Uri.
        /// </summary>
        /// <remarks>
        /// For example, uses 'localhost' from 'https://u:p@localhost:1234/a/b/c?a=1'.
        /// </remarks>
        UriHost = 1 << 3,

        /// <summary>
        /// The authority of the Uri.
        /// </summary>
        /// <remarks>
        /// For example, uses '1234' from 'https://u:p@localhost:1234/a/b/c?a=1'.
        /// </remarks>
        UriPort = 1 << 4,

        /// <summary>
        /// The authority of the Uri.
        /// </summary>
        /// <remarks>
        /// Uses both the <see cref="UriHost"/> and <see cref="UriPort"/>.
        /// </remarks>
        UriAuthority = UriHost | UriPort,

        /// <summary>
        /// The URI absolute path.
        /// </summary>
        /// <remarks>
        /// For example, uses 'a/b/c' from 'https://u:p@localhost:1234/a/b/c?a=1'.
        /// </remarks>
        UriPath = 1 << 5,

        /// <summary>
        /// The URI query.
        /// </summary>
        /// <remarks>
        /// For example, uses 'a=1' from 'https://u:p@localhost:1234/a/b/c?a=1'.
        /// </remarks>
        UriQuery = 1 << 6,

        /// <summary>
        /// The URI path and query
        /// </summary>
        UriPathAndQuery = UriPath | UriQuery,

        /// <summary>
        /// The full request uri, without the authority.
        /// </summary>
        /// <remarks>
        /// This includes just the schema, the path and the query.
        /// </remarks>
        UriWithoutAuthority = UriScheme | UriPathAndQuery,

        /// <summary>
        /// The full request URI.
        /// </summary>
        Uri = UriWithoutAuthority | UriUserInfo | UriAuthority,

        /// <summary>
        /// The request method.
        /// </summary>
        /// <remarks>
        /// For example, GET, PUT, POST, etc.
        /// </remarks>
        Method = 1 << 7,

        /// <summary>
        /// The request headers.
        /// </summary>
        Headers = 1 << 8,

        /// <summary>
        /// The request content.
        /// </summary>
        Content = 1 << 9,

        /// <summary>
        /// The entire response
        /// </summary>
        All = Uri | Method | Headers | Content | Version

        /*
         * Note we don't serialize or support request properties as these are specific to .NET and can realistically contain just about anything
         */
    }
}