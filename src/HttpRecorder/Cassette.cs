using System;
using System.Diagnostics;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using WebApplications.HttpRecorder.Exceptions;
using WebApplications.HttpRecorder.Internal;
using WebApplications.HttpRecorder.KeyGenerators;

[assembly: InternalsVisibleTo("HttpRecorder.Tests")]

namespace WebApplications.HttpRecorder
{
    /// <summary>
    /// Get's a <see cref="HttpResponseMessage"/> for a given <see cref="HttpResponseMessage"/>.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <returns></returns>
    public delegate HttpResponseMessage GetResponse(HttpRequestMessage request);

    /// <summary>
    /// Get's a <see cref="HttpResponseMessage"/> for a given <see cref="HttpResponseMessage"/> asynchronously.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <returns></returns>
    public delegate Task<HttpResponseMessage> GetResponseAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken = default(CancellationToken));

    /// <summary>
    /// A recorder is used to record messages sent using a <see cref="HttpClient" />.
    /// </summary>
    public class Cassette : IDisposable
    {
        /// <summary>
        /// The current key generator resolver.
        /// </summary>
        private static IKeyGeneratorResolver _keyGeneratorResolver = KeyGenerators.KeyGeneratorResolver.Instance;

        /// <summary>
        /// Gets the default <see cref="RecordingOptions"/> which are overwritten by any provided <see cref="RecordingOptions"/>.
        /// Note the <see cref="RecordingOptions.Mode">mode</see> will
        /// never be <see cref="RecordMode.Default"/> for the <see cref="DefaultOptions"/>
        /// </summary>
        /// <value>
        /// The default options.
        /// </value>
        public RecordingOptions DefaultOptions { get; }

        /// <summary>
        /// Gets or sets the key generator resolver.
        /// </summary>
        /// <value>
        /// The key generator.
        /// </value>
        public static IKeyGeneratorResolver KeyGeneratorResolver
        {
            get => _keyGeneratorResolver;
            set
            {
                if (value == null) value = KeyGenerators.KeyGeneratorResolver.Instance;
                _keyGeneratorResolver = value;
            }
        }

        /// <summary>
        /// Gets the key generator for this recorder.
        /// </summary>
        /// <value>
        /// The key generator.
        /// </value>
        public IKeyGenerator KeyGenerator { get; }

        /// <summary>
        /// Gets the cassette file path for this recorder.
        /// </summary>
        /// <value>
        /// The cassette file path.
        /// </value>
        public string Path { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Cassette" /> class.
        /// </summary>
        /// <param name="path">The cassette file path; optional, <see langword="null" /> to auto-generate file based on caller file path.</param>
        /// <param name="defaultOptions">The default options.</param>
        /// <param name="keyGenerator">The key generator resolver.</param>
        /// <param name="callerFilePath">The caller file path; set automatically, leave as <see langword="null" />.</param>
        public Cassette(
            string path = null,
            RecordingOptions defaultOptions = null,
            IKeyGenerator keyGenerator = null,
            [CallerFilePath] string callerFilePath = "")
        {
            // Overwrite default options, preventing the Mode from ever being RecordingOptions.Default
            DefaultOptions = RecordingOptions.Default.Combine(defaultOptions);

            if (path == null)
            {
                // Auto-generate cassette file path
                path = System.IO.Path.Combine(
                    System.IO.Path.GetDirectoryName(callerFilePath),
                    $"{System.IO.Path.GetFileNameWithoutExtension(callerFilePath)}.hrc");
            }

            if (keyGenerator is null) keyGenerator = KeyGeneratorResolver.Default;
            KeyGenerator = keyGenerator;

            Path = path;
        }

        // ReSharper disable ExplicitCallerInfoArgument
#pragma warning disable DF0001 // Marks undisposed anonymous objects from method invocations.
#pragma warning disable DF0000 // Marks undisposed anonymous objects from object creations.

        /// <summary>
        /// Gets a new <see cref="HttpClient" /> for recording.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="callerMemberName">Name of the caller member; set automatically, leave as <see langword="null" /></param>
        /// <param name="callerFilePath">The caller file path; set automatically, leave as <see langword="null" /></param>
        /// <returns></returns>
        public HttpClient GetClient(
            RecordingOptions options = null,
            [CallerMemberName] string callerMemberName = "",
            [CallerFilePath] string callerFilePath = "")
            => new HttpClient(
                new RecordingMessageHandler(
                    this,
                    null,
                    options,
                    callerMemberName),
                true);

        /// <summary>
        /// Gets a new <see cref="HttpMessageHandler" /> for recording, can be used to wrap an
        /// existing <see cref="HttpMessageHandler" /> before passing to a client.
        /// </summary>
        /// <param name="innerHandler">The inner handler; optional, defaults to creating a <see cref="HttpClientHandler" /> which is disposed when
        /// this instance is disposed, if specified, you will need to dispose the inner handler manually.</param>
        /// <param name="options">The options.</param>
        /// <param name="callerMemberName">Name of the caller member.</param>
        /// <param name="callerFilePath">The caller file path.</param>
        /// <returns></returns>
        public HttpMessageHandler GetHttpMessageHandler(
            HttpMessageHandler innerHandler = null,
            RecordingOptions options = null,
            [CallerMemberName] string callerMemberName = "",
            [CallerFilePath] string callerFilePath = "")
            => new RecordingMessageHandler(
                this,
                innerHandler,
                options,
                callerMemberName);

        /// <summary>
        /// Records/playbacks the <see cref="HttpResponseMessage" /> to specified <see cref="HttpRequestMessage" />. Should use
        /// <see cref="RecordAsync" /> if possible, this method is provided for convenience and wraps the asynchronous version.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="getResponse">The get response.</param>
        /// <param name="options">The options.</param>
        /// <param name="timeout">The timeout.</param>
        /// <param name="callerMemberName">Name of the caller member.</param>
        /// <returns>
        /// The <see cref="HttpResponseMessage" />.
        /// </returns>
        /// <exception cref="ArgumentNullException">request
        /// or
        /// request</exception>
        /// <exception cref="ArgumentOutOfRangeException">timeout</exception>
        public HttpResponseMessage Record(
            HttpRequestMessage request,
            GetResponse getResponse,
            RecordingOptions options = null,
            TimeSpan timeout = default(TimeSpan),
            [CallerMemberName] string callerMemberName = "")
        {
            if (request is null)
                throw new ArgumentNullException(nameof(request));
            if (getResponse is null)
                throw new ArgumentNullException(nameof(request));

            // Wrap to create 'async' version of function.
            Task<HttpResponseMessage> GetResponseAsync(HttpRequestMessage r, CancellationToken _) =>
                Task.FromResult(getResponse(r));


            if (timeout == default(TimeSpan))
                return RecordAsync(request, GetResponseAsync, options, CancellationToken.None, callerMemberName)
                    .Result;

            if (timeout <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(timeout));

            // Create cancellation token based on timeout
            using (CancellationTokenSource cts = new CancellationTokenSource(timeout))
            {
                return RecordAsync(request, GetResponseAsync, options, cts.Token, callerMemberName)
                    .Result;
            }
        }

#pragma warning restore DF0000 // Marks undisposed anonymous objects from object creations.
#pragma warning restore DF0001 // Marks undisposed anonymous objects from method invocations.

        // ReSharper restore ExplicitCallerInfoArgument

        /// <summary>
        /// Records/playbacks the <see cref="HttpResponseMessage" /> to specified <see cref="HttpRequestMessage" />.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="getResponseAsync">The function to call if a recording is needed.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="callerMemberName">Name of the caller member.</param>
        /// <returns>
        /// The <see cref="HttpResponseMessage" />.
        /// </returns>
        /// <exception cref="ArgumentNullException">request
        /// or
        /// request</exception>
        /// <exception cref="ArgumentOutOfRangeException">mode - null</exception>
        /// <exception cref="NotImplementedException"></exception>
        public async Task<HttpResponseMessage> RecordAsync(
            HttpRequestMessage request,
            GetResponseAsync getResponseAsync,
            RecordingOptions options = null,
            CancellationToken cancellationToken = default(CancellationToken),
            [CallerMemberName] string callerMemberName = "")
        {
            if (request is null)
                throw new ArgumentNullException(nameof(request));
            if (getResponseAsync is null)
                throw new ArgumentNullException(nameof(request));

            // Overwrite defaults with options
            options = DefaultOptions.Combine(options);
            RecordMode mode = options.Mode;
            Debug.Assert(mode != RecordMode.Default);

            // If we're in 'none' mode skip recording.
            if (mode == RecordMode.None)
                return await getResponseAsync(request, cancellationToken).ConfigureAwait(false);

            // Ensure request has been completed before serialization attempts
            if (!(request.Content is null))
                await request.Content.LoadIntoBufferAsync().ConfigureAwait(false);

            // Serialize the request data in full, using the full key generator
            byte[] requestData = FullRequestKeyGenerator.Instance.Generate(request);

            // Get key data, if we're not using the full request data for a key.
            byte[] key = ReferenceEquals(FullRequestKeyGenerator.Instance, KeyGenerator)
                ? requestData
                : KeyGenerator.Generate(request);

            // Get key hash.
            string hash = key.GetKeyHash();

            // TODO Find existing recording on cassette if any.
            Recording recording = null;

            // Should we record or playback?
            bool record;
            switch (options.Mode)
            {
                case RecordMode.Auto:
                    if (!(recording is null))
                        return recording.GetResponse();
                    break;

                case RecordMode.Playback:
                    if (recording is null)
                        throw new RecorderException(
                            $"No matching recording was found for the request.{Environment.NewLine}{request}");
                    return recording.GetResponse();

                // Record
                case RecordMode.Record:
                case RecordMode.Overwrite:
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(options), options.Mode,
                        "The specified mode is not valid!");
            }

            // We need to actually perform the request
            HttpResponseMessage response = await getResponseAsync(request, cancellationToken).ConfigureAwait(false);

            // If we have a recording and we're in record mode, don't overwrite just return the new response here.
            if (mode == RecordMode.Record && !(recording is null))
                return response;

            if (recording is null)
            {
                // TODO create new recording
            }
            else
            {
                // TODO update recording
            }

            // TODO Save recording

            return response;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            // TODO Flush
            //throw new NotImplementedException();
        }
    }
}
