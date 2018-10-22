using MessagePack;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using WebApplications.HttpRecorder.Exceptions;
using WebApplications.HttpRecorder.Internal;
using WebApplications.HttpRecorder.KeyGenerators;
using WebApplications.HttpRecorder.Logging;
using WebApplications.HttpRecorder.Serialization;
using WebApplications.HttpRecorder.Stores;

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
    public sealed class Cassette : IDisposable
    {
        private readonly bool _disposeStore;

        /// <summary>
        /// The current key generator resolver.
        /// </summary>
        private static IKeyGeneratorResolver _resolver = KeyGeneratorResolver.Instance;

        /// <summary>
        /// Gets or sets the key generator resolver.
        /// </summary>
        /// <value>
        /// The key generator.
        /// </value>
        public static IKeyGeneratorResolver Resolver
        {
            get => _resolver;
            set
            {
                if (value is null) value = KeyGeneratorResolver.Instance;
                _resolver = value;
            }
        }

        /// <summary>
        /// The keyed semaphore slim allows locking based on request hash.
        /// </summary>
        private readonly KeyedSemaphoreSlim _keyedSemaphoreSlim = new KeyedSemaphoreSlim();

        /// <summary>
        /// Gets the default <see cref="CassetteOptions" /> which are overwritten by any provided <see cref="CassetteOptions" />.
        /// </summary>
        /// <value>
        /// The default options.
        /// </value>
        public CassetteOptions DefaultOptions { get; }

        /// <summary>
        /// Gets the underlying <see cref="ICassetteStore">cassette store</see>.
        /// </summary>
        /// <value>
        /// The cassette store.
        /// </value>
        public ICassetteStore Store { get; }

        /// <summary>
        /// Gets the key generator for this recorder.
        /// </summary>
        /// <value>
        /// The key generator.
        /// </value>
        public IKeyGenerator KeyGenerator { get; }

        /// <summary>
        /// Gets the logger.
        /// </summary>
        /// <value>
        /// The logger.
        /// </value>
        public IRecorderLogger Logger { get; }

        /// <summary>
        /// Gets the cassette name for this recorder.
        /// </summary>
        /// <value>
        /// The cassette name.
        /// </value>
        public string Name => Store.Name;

        /// <summary>
        /// Initializes a new instance of the <see cref="Cassette" /> class, with storage provided
        /// by a <see cref="FileStore">single file</see> located alongside the caller source file.
        /// </summary>
        /// <param name="defaultOptions">The default options.</param>
        /// <param name="keyGenerator">The key generator resolver.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="callerFilePath">The caller name; set automatically, leave as <see langword="null" />.</param>
        public Cassette(
            CassetteOptions defaultOptions = null,
            IKeyGenerator keyGenerator = null,
            IRecorderLogger logger = null,
            [CallerFilePath] string callerFilePath = "")
            : this(
#pragma warning disable DF0000 // Marks undisposed anonymous objects from object creations.
                new FileStore(Path.Combine(
                    Path.GetDirectoryName(callerFilePath),
                    Path.GetFileNameWithoutExtension(callerFilePath) + "_cassette" + FileStore.DefaultExtension)),
#pragma warning restore DF0000 // Marks undisposed anonymous objects from object creations.
                defaultOptions,
                keyGenerator,
                logger,
                true)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Cassette" /> class.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <param name="defaultOptions">The default options.</param>
        /// <param name="keyGenerator">The key generator resolver.</param>
        /// <param name="logger">The logger.</param>
        public Cassette(
            string filePath,
            CassetteOptions defaultOptions = null,
            IKeyGenerator keyGenerator = null,
            IRecorderLogger logger = null)
            : this(
#pragma warning disable DF0000 // Marks undisposed anonymous objects from object creations.
                new FileStore(filePath),
#pragma warning restore DF0000 // Marks undisposed anonymous objects from object creations.
                defaultOptions,
                keyGenerator,
                logger,
                true)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Cassette" /> class.
        /// </summary>
        /// <param name="store">The underlying store for this cassette.</param>
        /// <param name="defaultOptions">The default options.</param>
        /// <param name="keyGenerator">The key generator resolver.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="disposeStore">if set to <c>true</c> the cassette will dispose the store when it is disposed.</param>
        /// <exception cref="ArgumentNullException">store</exception>
        public Cassette(
            ICassetteStore store,
            CassetteOptions defaultOptions = null,
            IKeyGenerator keyGenerator = null,
            IRecorderLogger logger = null,
            bool disposeStore = false)
        {
            Store = store ?? throw new ArgumentNullException(nameof(store));
            // Overwrite default options, preventing the Mode from ever being CassetteOptions.Default
            DefaultOptions = CassetteOptions.Default & defaultOptions;
            if (keyGenerator is null) keyGenerator = Resolver.Default;
            KeyGenerator = keyGenerator;
            Logger = logger;
            _disposeStore = disposeStore;
        }

        // ReSharper disable ExplicitCallerInfoArgument
#pragma warning disable DF0001 // Marks undisposed anonymous objects from method invocations.
#pragma warning disable DF0000 // Marks undisposed anonymous objects from object creations.

        /// <summary>
        /// Gets a new <see cref="HttpClient" /> for recording.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="callerFilePath">The caller file path; set automatically.</param>
        /// <param name="callerMemberName">Name of the caller member; set automatically.</param>
        /// <param name="callerLineNumber">The caller line number; set automatically.</param>
        /// <returns></returns>
        public HttpClient GetClient(
            CassetteOptions options = null,
            [CallerFilePath] string callerFilePath = "",
            [CallerMemberName] string callerMemberName = "",
            [CallerLineNumber] int callerLineNumber = 0)
            => new HttpClient(
                new RecordingMessageHandler(
                    this,
                    null,
                    options,
                    callerFilePath,
                    callerMemberName,
                    callerLineNumber),
                true);

        /// <summary>
        /// Gets a new <see cref="HttpMessageHandler" /> for recording, can be used to wrap an
        /// existing <see cref="HttpMessageHandler" /> before passing to a client.
        /// </summary>
        /// <param name="innerHandler">The inner handler; optional, defaults to creating a <see cref="HttpClientHandler" /> which is disposed when
        /// this instance is disposed, if specified, you will need to dispose the inner handler manually.</param>
        /// <param name="options">The options.</param>
        /// <param name="callerFilePath">The caller file path; set automatically.</param>
        /// <param name="callerMemberName">Name of the caller member; set automatically.</param>
        /// <param name="callerLineNumber">The caller line number; set automatically.</param>
        /// <returns></returns>
        public HttpMessageHandler GetHttpMessageHandler(
            HttpMessageHandler innerHandler = null,
            CassetteOptions options = null,
            [CallerFilePath] string callerFilePath = "",
            [CallerMemberName] string callerMemberName = "",
            [CallerLineNumber] int callerLineNumber = 0)
            => new RecordingMessageHandler(
                this,
                innerHandler,
                options,
                callerFilePath,
                callerMemberName,
                callerLineNumber);

        /// <summary>
        /// Records/playbacks the <see cref="HttpResponseMessage" /> to specified <see cref="HttpRequestMessage" />.
        /// </summary>
        /// <param name="response">The response.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="callerFilePath">The caller file path; set automatically.</param>
        /// <param name="callerMemberName">Name of the caller member; set automatically.</param>
        /// <param name="callerLineNumber">The caller line number; set automatically.</param>
        /// <returns>
        /// The <see cref="HttpResponseMessage" />.
        /// </returns>
        /// <exception cref="ArgumentNullException">request
        /// or
        /// request</exception>
        /// <exception cref="ArgumentOutOfRangeException">mode - null</exception>
        public Task<HttpResponseMessage> RecordAsync(
            HttpResponseMessage response,
            CancellationToken cancellationToken = default(CancellationToken),
            [CallerFilePath] string callerFilePath = "",
            [CallerMemberName] string callerMemberName = "",
            [CallerLineNumber] int callerLineNumber = 0)
            => RecordAsync(
                response.RequestMessage,
                (r, ct) => Task.FromResult(response), null, cancellationToken, callerFilePath,
                callerMemberName,
                callerLineNumber);

        /// <summary>
        /// Records/playbacks the <see cref="HttpResponseMessage" /> to specified <see cref="HttpRequestMessage" />.
        /// </summary>
        /// <param name="response">The response.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="callerFilePath">The caller file path; set automatically.</param>
        /// <param name="callerMemberName">Name of the caller member; set automatically.</param>
        /// <param name="callerLineNumber">The caller line number; set automatically.</param>
        /// <returns>
        /// The <see cref="HttpResponseMessage" />.
        /// </returns>
        /// <exception cref="ArgumentNullException">request
        /// or
        /// request</exception>
        /// <exception cref="ArgumentOutOfRangeException">mode - null</exception>
        public Task<HttpResponseMessage> RecordAsync(
            HttpResponseMessage response,
            CassetteOptions options = null,
            CancellationToken cancellationToken = default(CancellationToken),
            [CallerFilePath] string callerFilePath = "",
            [CallerMemberName] string callerMemberName = "",
            [CallerLineNumber] int callerLineNumber = 0)
            => RecordAsync(
                response.RequestMessage,
                (r, ct) => Task.FromResult(response), options, cancellationToken, callerFilePath,
                callerMemberName,
                callerLineNumber);

        /// <summary>
        /// Records/playbacks the <see cref="HttpResponseMessage" /> to specified <see cref="HttpRequestMessage" />.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="response">The response.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="callerFilePath">The caller file path; set automatically.</param>
        /// <param name="callerMemberName">Name of the caller member; set automatically.</param>
        /// <param name="callerLineNumber">The caller line number; set automatically.</param>
        /// <returns>
        /// The <see cref="HttpResponseMessage" />.
        /// </returns>
        /// <exception cref="ArgumentNullException">request
        /// or
        /// request</exception>
        /// <exception cref="ArgumentOutOfRangeException">mode - null</exception>
        public Task<HttpResponseMessage> RecordAsync(
            HttpRequestMessage request,
            HttpResponseMessage response,
            CancellationToken cancellationToken = default(CancellationToken),
            [CallerFilePath] string callerFilePath = "",
            [CallerMemberName] string callerMemberName = "",
            [CallerLineNumber] int callerLineNumber = 0)
            => RecordAsync(
                request,
                (r, ct) => Task.FromResult(response), null, cancellationToken, callerFilePath,
                callerMemberName,
                callerLineNumber);

        /// <summary>
        /// Records/playbacks the <see cref="HttpResponseMessage" /> to specified <see cref="HttpRequestMessage" />.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="response">The response.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="callerFilePath">The caller file path; set automatically.</param>
        /// <param name="callerMemberName">Name of the caller member; set automatically.</param>
        /// <param name="callerLineNumber">The caller line number; set automatically.</param>
        /// <returns>
        /// The <see cref="HttpResponseMessage" />.
        /// </returns>
        /// <exception cref="ArgumentNullException">request
        /// or
        /// request</exception>
        /// <exception cref="ArgumentOutOfRangeException">mode - null</exception>
        public Task<HttpResponseMessage> RecordAsync(
            HttpRequestMessage request,
            HttpResponseMessage response,
            CassetteOptions options = null,
            CancellationToken cancellationToken = default(CancellationToken),
            [CallerFilePath] string callerFilePath = "",
            [CallerMemberName] string callerMemberName = "",
            [CallerLineNumber] int callerLineNumber = 0)
            => RecordAsync(
                request,
                (r, ct) => Task.FromResult(response), options, cancellationToken, callerFilePath,
                callerMemberName,
                callerLineNumber);

        /// <summary>
        /// Records/playbacks the <see cref="HttpResponseMessage" /> to specified <see cref="HttpRequestMessage" />.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="getResponseAsync">The function to call if a recording is needed.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="callerFilePath">The caller file path; set automatically.</param>
        /// <param name="callerMemberName">Name of the caller member; set automatically.</param>
        /// <param name="callerLineNumber">The caller line number; set automatically.</param>
        /// <returns>
        /// The <see cref="HttpResponseMessage" />.
        /// </returns>
        /// <exception cref="ArgumentNullException">request
        /// or
        /// request</exception>
        /// <exception cref="ArgumentOutOfRangeException">mode - null</exception>
        public Task<HttpResponseMessage> RecordAsync(
            HttpRequestMessage request,
            GetResponseAsync getResponseAsync,
            CancellationToken cancellationToken,
            [CallerFilePath] string callerFilePath = "",
            [CallerMemberName] string callerMemberName = "",
            [CallerLineNumber] int callerLineNumber = 0)
            => RecordAsync(request, getResponseAsync, null, cancellationToken, callerFilePath, callerMemberName,
                callerLineNumber);

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
        /// <param name="callerFilePath">The caller file path; set automatically.</param>
        /// <param name="callerMemberName">Name of the caller member; set automatically.</param>
        /// <param name="callerLineNumber">The caller line number; set automatically.</param>
        /// <returns>
        /// The <see cref="HttpResponseMessage" />.
        /// </returns>
        /// <exception cref="ArgumentNullException">request
        /// or
        /// request</exception>
        /// <exception cref="ArgumentOutOfRangeException">mode - null</exception>
        public async Task<HttpResponseMessage> RecordAsync(
            HttpRequestMessage request,
            GetResponseAsync getResponseAsync,
            CassetteOptions options = null,
            CancellationToken cancellationToken = default(CancellationToken),
            [CallerFilePath] string callerFilePath = "",
            [CallerMemberName] string callerMemberName = "",
            [CallerLineNumber] int callerLineNumber = 0)
        {
            if (request is null)
                throw new ArgumentNullException(nameof(request));
            if (getResponseAsync is null)
                throw new ArgumentNullException(nameof(request));

            // Overwrite defaults with options
            options = DefaultOptions & options;
            // ReSharper disable once PossibleInvalidOperationException
            RecordMode mode = options.Mode.Value;

            // If we're in 'none' mode skip recording.
            if (mode == RecordMode.None)
                return await getResponseAsync(request, cancellationToken).ConfigureAwait(false);

            // Ensure request has been completed before serialization attempts
            if (!(request.Content is null))
                await request.Content.LoadIntoBufferAsync().ConfigureAwait(false);

            // Get key data.
            byte[] key = KeyGenerator.Generate(request);

            // Get key data hash.
            string hash = key.GetKeyHash();

            /*
             * Lock based on hash - so only one operation is allowed for the same hash at the same time.
             */
            IDisposable @lock = await _keyedSemaphoreSlim.WaitAsync(hash, cancellationToken);
            try
            {
                // Try to get recording from the store.
                Recording recording;
                bool found = false;
                HttpResponseMessage response = null;
                byte[] recordingData = null;

                /*
                 * Unless we're in overwrite mode, try to get a response from the store.
                 */
                if (mode != RecordMode.Overwrite)
                {
                    // Logs an error
                    // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
                    void Error(string message, Exception exception = null, bool @throw = false)
                    {
                        CassetteException cassetteException = new CassetteException(
                            message,
                            Store.Name,
                            callerFilePath,
                            callerMemberName,
                            callerLineNumber,
                            exception);

                        if (@throw)
                        {
                            Logger.LogCritical(cassetteException);
                            throw cassetteException;
                        }

                        Logger.LogError(cassetteException);
                        recording = null;
                        found = false;
                    }

                    try
                    {
                        recordingData = await Store.GetAsync(hash, cancellationToken);
                    }
                    catch (Exception e)
                    {
                        Error("The underlying store threw an exception when attempting to retrieve a recording.", e);
                    }

                    // If we got a response and it has more than 0 bytes consider it fond!
                    if (!(recordingData is null) && recordingData.Length > 0)
                    {

                        found = true;

                        // If we're in recording mode don't bother to deserialize it as we're not going to use it
                        if (mode != RecordMode.Record)
                        {
                            // Deserialize recording
                            try
                            {
                                recording = MessagePackSerializer.Deserialize<Recording>(recordingData,
                                    RecorderResolver.Instance);
                            }
                            catch (Exception e)
                            {
                                Error("The recording could not be deserialized.", e);
                            }

                            // Validate key
                            if (found && !string.Equals(hash, recording.Hash))
                                Error("The recording's hash did not match, ignoring.");
                            if (found && !string.Equals(KeyGenerator.Name, recording.KeyGeneratorName))
                                Error("The recording's key generator name did not match, ignoring.");

                            /*
                             * If we're in playback or auto mode we need to replay response
                             */
                            if (found && (mode == RecordMode.Playback || mode == RecordMode.Auto))
                            {
                                if (recording.ResponseData is null)
                                    Error("No response data found in recording, ignoring.");



                                // Deserialize response
                                if (found)
                                    try
                                    {
                                        response = MessagePackSerializer.Deserialize<HttpResponseMessage>(
                                            recording.ResponseData, RecorderResolver.Instance);
                                    }
                                    catch (Exception e)
                                    {
                                        Error("Failed to deserialize the response from the store, ignoring.", e);
                                    }

                                // ReSharper disable once PossibleInvalidOperationException
                                RequestPlaybackMode requestPlaybackMode = options.RequestPlaybackMode.Value;
                                if (found)
                                {
                                    if (recording.RequestData is null ||
                                        requestPlaybackMode != RequestPlaybackMode.IgnoreRecorded)
                                    {
                                        if (requestPlaybackMode == RequestPlaybackMode.UseRecorded)
                                            Error(
                                                "No request found in the recording, and in RequestPlaybackMode UseRecorded.",
                                                null,
                                                true);

                                        // ReSharper disable once PossibleNullReferenceException
                                        response.RequestMessage = request;
                                    }
                                    else
                                    {
                                        // Deserialize request
                                        try
                                        {
#pragma warning disable DF0023 // Marks undisposed objects assinged to a property, originated from a method invocation.
                                            // ReSharper disable once PossibleNullReferenceException
                                            response.RequestMessage =
                                                MessagePackSerializer.Deserialize<HttpRequestMessage>(
                                                    recording.RequestData, RecorderResolver.Instance);
#pragma warning restore DF0023 // Marks undisposed objects assinged to a property, originated from a method invocation.
                                        }
                                        catch (Exception e)
                                        {
                                            if (requestPlaybackMode == RequestPlaybackMode.UseRecorded)
                                                Error(
                                                    "Failed to deserialize the request from the store, and in RequestPlaybackMode UseRecorded.",
                                                    e,
                                                    true);
                                            else
                                                Error("Failed to deserialize the request from the store, ignoring.", e);
                                        }
                                    }
                                }

                                // ReSharper disable once PossibleInvalidOperationException
                                if (found)
                                {
                                    TimeSpan simulateDelay = options.SimulateDelay.Value;
                                    if (simulateDelay != default(TimeSpan))
                                    {
                                        int delay = simulateDelay < TimeSpan.Zero
                                            ? recording.DurationMs
                                            : (int)simulateDelay.TotalMilliseconds;

                                        Logger.LogInformation(
                                            $"Responding with matching recording from '{recording.RecordedUtc.ToLocalTime()}' after {delay}ms simulated delay.",
                                            Store.Name,
                                            callerFilePath,
                                            callerMemberName,
                                            callerLineNumber);

                                        await Task.Delay(delay, cancellationToken);
                                    }
                                    else
                                        Logger.LogInformation(
                                            $"Responding with matching recording from '{recording.RecordedUtc.ToLocalTime()}'.",
                                            Store.Name,
                                            callerFilePath,
                                            callerMemberName,
                                            callerLineNumber);

                                    return response;
                                }
                            }
                        }
                    }
                }

                // If we're in playback mode we've failed to get a recording so error
                if (mode == RecordMode.Playback)
                {
                    // Recording not found so error in playback mode.
                    CassetteNotFoundException exception = new CassetteNotFoundException(
                        Store.Name,
                        callerFilePath,
                        callerMemberName,
                        callerLineNumber);
                    Logger.LogError(exception);
                    throw exception;
                }

                /*
                 * Record original request to detect changes if options set to RequestRecordMode.RecordIfChanged
                 */
                byte[] requestData;
                // ReSharper disable once PossibleInvalidOperationException
                RequestRecordMode requestRecordMode = options.RequestRecordMode.Value;
                if (!found && requestRecordMode == RequestRecordMode.RecordIfChanged)
                {
                    // If the key was generated with the FullRequestKeyGenerator.Instance then the request is already serialized.
                    if (ReferenceEquals(KeyGenerator, FullRequestKeyGenerator.Instance))
                        requestData = key;
                    else
                    {
                        try
                        {
                            requestData = MessagePackSerializer.Serialize(request, RecorderResolver.Instance);
                        }
                        catch (Exception e)
                        {
                            CassetteException ce = new CassetteException(
                                "Failed to serialize the request.",
                                Store.Name,
                                callerFilePath,
                                callerMemberName,
                                callerLineNumber,
                                e);
                            Logger.LogCritical(ce);
                            throw ce;
                        }
                    }
                }
                else
                    requestData = null;

                /*
                 * Retrieve response from endpoint.
                 */
                int durationMs;
                DateTime recordedUtc;
                try
                {
                    // Use stopwatch to record how long it takes to get a response.
                    Stopwatch stopwatch = Stopwatch.StartNew();
#pragma warning disable DF0010 // Marks undisposed local variables.
                    response = await getResponseAsync(request, cancellationToken).ConfigureAwait(false);
#pragma warning restore DF0010 // Marks undisposed local variables.
                    durationMs = (int)stopwatch.ElapsedMilliseconds;
                    recordedUtc = DateTime.UtcNow;
                }
                catch (Exception e)
                {
                    // TODO We could save the exception an repeat on playback, useful for testing handlers
                    // Unfortunately MessagePack-CSharp doesn't support exception serialization normally so would need to be
                    // handled in a custom way.
                    CassetteException re = new CassetteException("Fatal error occured retrieving the response.",
                        Store.Name,
                        callerFilePath,
                        callerMemberName,
                        callerLineNumber,
                        e);
                    Logger.LogError(re);
                    throw re;
                }

                // If we have a recording, don't overwrite just return the new response here.
                if (found)
                {
                    Logger.LogInformation(
                        "Existing recording found so not overwriting it.",
                        Store.Name,
                        callerFilePath,
                        callerMemberName,
                        callerLineNumber);
                    return response;
                }


                // Serialize response
                byte[] responseData;
                try
                {
                    responseData = MessagePackSerializer.Serialize(response, RecorderResolver.Instance);
                }
                catch (Exception e)
                {
                    CassetteException re = new CassetteException(
                        "Failed to serialize response, not storing.",
                        Store.Name,
                        callerFilePath,
                        callerMemberName,
                        callerLineNumber,
                        e);
                    Logger.LogError(re);
                    return response;
                }


                if (requestRecordMode != RequestRecordMode.Ignore)
                {
                    byte[] oldRequestData = requestData;
                    // Serialize the request
                    try
                    {
                        requestData =
                            MessagePackSerializer.Serialize(response.RequestMessage, RecorderResolver.Instance);

                        // If we're only recording requests on change, check for changes
                        if (requestRecordMode == RequestRecordMode.RecordIfChanged &&
                            // ReSharper disable once AssignNullToNotNullAttribute
                            requestData.SequenceEqual(oldRequestData))
                            requestData = null;
                    }
                    catch (Exception e)
                    {
                        CassetteException re = new CassetteException(
                            "Failed to serialize response's request message, so ignoring check.",
                            Store.Name,
                            callerFilePath,
                            callerMemberName,
                            callerLineNumber,
                            e);
                        Logger.LogError(re);
                        requestData = null;
                    }
                }

                // Create new recording
                recording = new Recording(
                    hash,
                    KeyGenerator.Name,
                    recordedUtc,
                    durationMs,
                    responseData,
                    requestData);

                // Finally serialize the recording
                try
                {
                    recordingData = MessagePackSerializer.Serialize(recording, RecorderResolver.Instance);
                }
                catch (Exception e)
                {
                    CassetteException re = new CassetteException(
                        "Failed to serialize recording, not storing.",
                        Store.Name,
                        callerFilePath,
                        callerMemberName,
                        callerLineNumber,
                        e);
                    Logger.LogError(re);
                    return response;
                }

                // Set the response
                Logger.LogInformation(
                    $"Recording response at '{recordedUtc.ToLocalTime()}' (took {durationMs} ms).",
                    Store.Name,
                    callerFilePath,
                    callerMemberName,
                    callerLineNumber);

                if (options.WaitForSave == true)
                {
                    try
                    {
                        await Store.StoreAsync(hash, recordingData);
                    }
                    catch (Exception e)
                    {
                        // Just log the error.
                        CassetteException re = new CassetteException(
                            "Failed to store recording.",
                            Store.Name,
                            callerFilePath,
                            callerMemberName,
                            callerLineNumber,
                            e);
                        Logger.LogError(re);
                    }

                    // We can now dispose the lock safely.
                    @lock.Dispose();
                }
                else
                    // Store the recording asynchronously, and don't wait the result (errors will be logged and suppressed).
                    StoreAsync(hash, recordingData, @lock, callerFilePath, callerMemberName, callerLineNumber);

                // Return the response.
                return response;
            }
            catch
            {
                // If we're throwing an exception dispose the lock.
                @lock.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Stores the recording asynchronously without being waited for.
        /// </summary>
        /// <param name="hash">The hash.</param>
        /// <param name="recordingData">The recording data.</param>
        /// <param name="lock">The @lock.</param>
        /// <param name="callerFilePath">The caller file path.</param>
        /// <param name="callerMemberName">Name of the caller member.</param>
        /// <param name="callerLineNumber">The caller line number.</param>
        private async void StoreAsync(
                    string hash,
                    byte[] recordingData,
                    IDisposable @lock,
                    string callerFilePath,
                    string callerMemberName,
                    int callerLineNumber)
        {
            try
            {
                await Store.StoreAsync(hash, recordingData);
            }
            catch (Exception ex)
            {

                // Just log the error.
                CassetteException re = new CassetteException(
                    "Failed to store recording.",
                    Store.Name,
                    callerFilePath,
                    callerMemberName,
                    callerLineNumber,
                    ex);
                Logger.LogError(re);
            }
            finally
            {
                // We don't release the hash lock until storage is complete, even though the underlying response
                // has already been returned.
                @lock.Dispose();
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _keyedSemaphoreSlim.Dispose();
            if (_disposeStore)
                Store.Dispose();
        }
    }
}