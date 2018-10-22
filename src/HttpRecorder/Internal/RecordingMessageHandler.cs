using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace WebApplications.HttpRecorder.Internal
{
    /// <summary>
    /// Used to instrument a <see cref="HttpClient"/> for recording and playback.
    /// </summary>
    /// <seealso cref="System.Net.Http.DelegatingHandler" />
    internal class RecordingMessageHandler : DelegatingHandler
    {
        private readonly Cassette _cassette;
        private readonly CassetteOptions _options;
        private readonly string _callerFilePath;
        private readonly string _callerMemberName;
        private readonly int _callerLineNumber;

        private int _disposeInner;

        /// <summary>
        /// Gets a new <see cref="HttpMessageHandler" /> for recording, can be used to wrap an
        /// existing <see cref="HttpMessageHandler" /> before passing to a client
        /// </summary>
        /// <param name="cassette">The recorder.</param>
        /// <param name="innerHandler">The inner handler; optional, defaults to creating a new <see cref="HttpClientHandler" />.</param>
        /// <param name="options">The options.</param>
        /// <param name="callerFilePath">The caller file path.</param>
        /// <param name="callerMemberName">Name of the caller member.</param>
        /// <param name="callerLineNumber">The caller line number.</param>
        /// <returns></returns>
        public RecordingMessageHandler(
            Cassette cassette,
            HttpMessageHandler innerHandler,
            CassetteOptions options,
            string callerFilePath,
            string callerMemberName,
            int callerLineNumber)
        {
            _cassette = cassette;
            _options = options;
            _callerFilePath = callerFilePath;
            _callerMemberName = callerMemberName;
            _callerLineNumber = callerLineNumber;

#pragma warning disable DF0020 // Marks undisposed objects assinged to a field, originated in an object creation.
            if (innerHandler is null)
            {
                innerHandler = new HttpClientHandler();
                _disposeInner = 1;
            }
#pragma warning restore DF0020 // Marks undisposed objects assinged to a field, originated in an object creation.
            InnerHandler = innerHandler;
        }

        /// <summary>
        /// Sends an HTTP request to the inner handler to send to the server as an asynchronous operation.
        /// </summary>
        /// <param name="request">The HTTP request message to send to the server.</param>
        /// <param name="cancellationToken">A cancellation token to cancel operation.</param>
        /// <returns>
        /// Returns <see cref="T:System.Threading.Tasks.Task`1" />. The task object representing the asynchronous operation.
        /// </returns>
        // ReSharper disable ExplicitCallerInfoArgument
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
            => _cassette.RecordAsync(request, base.SendAsync, _options, cancellationToken, _callerFilePath, _callerMemberName, _callerLineNumber);
        // ReSharper restore ExplicitCallerInfoArgument

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="T:System.Net.Http.DelegatingHandler" />, and optionally disposes of the managed resources.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to releases only unmanaged resources.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && Interlocked.CompareExchange(ref _disposeInner, 0, 1) == 1)
                InnerHandler.Dispose();

            base.Dispose(disposing);
        }
    }
}