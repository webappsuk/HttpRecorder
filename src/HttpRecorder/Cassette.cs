using MessagePack;
using System;
using System.Diagnostics;
using System.IO;
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
                if (value == null) value = KeyGeneratorResolver.Instance;
                _resolver = value;
            }
        }

        /// <summary>
        /// The keyed semaphore slim allows locking based on request hash.
        /// </summary>
        private readonly KeyedSemaphoreSlim _keyedSemaphoreSlim = new KeyedSemaphoreSlim();

        /// <summary>
        /// Gets the default <see cref="CassetteOptions"/> which are overwritten by any provided <see cref="CassetteOptions"/>.
        /// Note the <see cref="CassetteOptions.Mode">mode</see> will
        /// never be <see cref="RecordMode.Default"/> for the <see cref="DefaultOptions"/>
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
            DefaultOptions = CassetteOptions.Default.Combine(defaultOptions);
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
            options = DefaultOptions.Combine(options);
            RecordMode mode = options.Mode;
            Debug.Assert(mode != RecordMode.Default);

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

            // Get lock for hash.
            using (await _keyedSemaphoreSlim.WaitAsync(hash, cancellationToken))
            {
                // Try to get recording from the store.
                byte[] responseData = null;
                bool found = false;

                // Unless we're in overwrite mode, try to get a response from the store.
                if (mode != RecordMode.Overwrite)
                {
                    try
                    {
                        responseData = await Store.GetAsync(hash, cancellationToken);
                    }
                    catch (Exception e)
                    {
                        CassetteException re = new CassetteException(
                            "The underlying store threw an exception when attempting to retrieve a recording.",
                            Store.Name,
                            callerFilePath,
                            callerMemberName,
                            callerLineNumber,
                            e);
                        Logger.LogError(re);
                        throw re;
                    }

                    found = !(responseData is null);
                    if (found && responseData.Length < 1)
                    {
                        found = false;
                        responseData = null;
                    }
                }

                HttpResponseMessage response;
                switch (mode)
                {
                    case RecordMode.Playback:
                    case RecordMode.Auto:
                        if (found)
                        {
                            Logger.LogInformation(
                                "Responding with matching recording.",
                                Store.Name,
                                callerFilePath,
                                callerMemberName,
                                callerLineNumber);

                            try
                            {
                                response = MessagePackSerializer.Deserialize<HttpResponseMessage>(responseData,
                                    RecorderResolver.Instance);
                                // Set the request
                                response.RequestMessage = request;
                                return response;
                            }
                            catch (Exception e)
                            {
                                CassetteException re = new CassetteException(
                                    "Failed to deserialize retrieved recording.",
                                    Store.Name,
                                    callerFilePath,
                                    callerMemberName,
                                    callerLineNumber,
                                    e);
                                Logger.LogError(re);

                                // Fall-through to allow new recording
                                found = false;
                            }
                        }

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

                        break;

                    case RecordMode.Overwrite:
                        break;

                    case RecordMode.Record:
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(options), mode,
                            "The specified mode is not valid!");
                }

                // We're now ready to get the response.
                try
                {
                    response = await getResponseAsync(request, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    // TODO We could save the exception an repeat on playback, useful for testing handlers
                    // At same time we should serialize extra info like execution duration, and key generator name.
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
                        "Existing recording found.",
                        Store.Name,
                        callerFilePath,
                        callerMemberName,
                        callerLineNumber);
                    return response;
                }

                // Serialize new response
                try
                {
                    responseData = MessagePackSerializer.Serialize(response, RecorderResolver.Instance);
                }
                catch (Exception e)
                {
                    CassetteException re = new CassetteException(
                        "Failed to serialize HttpResponse.",
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
                    "Recording response.",
                    Store.Name,
                    callerFilePath,
                    callerMemberName,
                    callerLineNumber);

                if (options.WaitForSave == true)
                    try
                    {
                        await Store.StoreAsync(hash, responseData);
                    }
                    catch (Exception e)
                    {
                        // Just log the error.
                        CassetteException re = new CassetteException(
                            "Failed to store response.",
                            Store.Name,
                            callerFilePath,
                            callerMemberName,
                            callerLineNumber,
                            e);
                        Logger.LogError(re);
                    }
                else
                    Store.StoreAsync(hash, responseData)
                        .FireAndForget(e =>
                        {
                            // Just log the error.
                            CassetteException re = new CassetteException(
                                "Failed to store response.",
                                Store.Name,
                                callerFilePath,
                                callerMemberName,
                                callerLineNumber,
                                e);
                            Logger.LogError(re);
                        });

                // Return the response.
                return response;
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