using MessagePack;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using WebApplications.HttpRecorder.Serialization;
using Xunit.Abstractions;

namespace WebApplications.HttpRecorder.Tests.Helpers
{
    public class TestHttpMessageHandler : DelegatingHandler
    {
        /// <summary>
        /// Gets or sets the action.
        /// </summary>
        /// <value>
        /// The action.
        /// </value>
        public Action<HttpResponseMessage> Action { get; set; }

        /// <summary>
        /// Gets or sets the status code.
        /// </summary>
        /// <value>
        /// The status code.
        /// </value>
        public HttpStatusCode StatusCode { get; set; }

        /// <summary>
        /// Gets or sets the reason phrase.
        /// </summary>
        /// <value>
        /// The reason phrase.
        /// </value>
        public string ReasonPhrase { get; set; }

        /// <summary>
        /// Gets or sets the version.
        /// </summary>
        /// <value>
        /// The version.
        /// </value>
        public Version Version
        {
            get => _version;
            set
            {
                if (value == null) value = HttpVersion.Version20;
                _version = value;
            }
        }

        private readonly ITestOutputHelper _output;
        private Version _version;

        /// <summary>
        /// Gets the call count.
        /// </summary>
        /// <value>
        /// The call count.
        /// </value>
        public int CallCount { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TestHttpMessageHandler" /> class.
        /// </summary>
        /// <param name="output">The output.</param>
        /// <param name="action">The action.</param>
        /// <param name="statusCode">The status code.</param>
        /// <param name="reasonPhrase">The reason phrase.</param>
        /// <param name="version">The version.</param>
        public TestHttpMessageHandler(
            ITestOutputHelper output,
            Action<HttpResponseMessage> action = null,
            HttpStatusCode statusCode = HttpStatusCode.OK,
            string reasonPhrase = null,
            Version version = null)
        {
            Action = action;
            StatusCode = statusCode;
            ReasonPhrase = reasonPhrase;
            Version = version;
            _output = output;
        }

        /// <inheritdoc />
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            HttpResponseMessage response = new HttpResponseMessage(StatusCode)
            {
                ReasonPhrase = ReasonPhrase ?? StatusCode.ToString(),
                Version = Version,
                RequestMessage = request
            };

            // Default action is to create a stream response reflecting back the request content (if any).
            if (!(request.Content is null))
            {
#pragma warning disable DF0001 // Marks undisposed anonymous objects from method invocations.
#pragma warning disable DF0022 // Marks undisposed objects assinged to a property, originated in an object creation.
                response.Content = new StreamContent(await request.Content.ReadAsStreamAsync());
#pragma warning restore DF0022 // Marks undisposed objects assinged to a property, originated in an object creation.
#pragma warning restore DF0001 // Marks undisposed anonymous objects from method invocations.
                foreach (var header in request.Content.Headers)
                {
                    response.Content.Headers.Add(header.Key, header.Value);
                }
            }

            CallCount++;

            byte[] requestData = MessagePackSerializer.Serialize(request, RecorderResolver.Instance);
            _output.WriteLine("Test Handler Received Request:");
            _output.WriteLine(MessagePackSerializer.ToJson(requestData));
            _output.WriteLine(string.Empty);

            Action<HttpResponseMessage> action = Action;
            action?.Invoke(response);

            byte[] responseData = MessagePackSerializer.Serialize(response, RecorderResolver.Instance);
            _output.WriteLine("Responding with:");
            _output.WriteLine(MessagePackSerializer.ToJson(responseData));

            if (action is null)
                return response;

            // Check if request has been mangled by action.
            if (response.RequestMessage == null)
                _output.WriteLine("Response.RequestMessage is null!");
            else
            {
                byte[] responseRequestData = MessagePackSerializer.Serialize(request, RecorderResolver.Instance);
                if (!requestData.SequenceEqual(responseRequestData))
                {
                    _output.WriteLine("Response.RequestMessage was changed to:");
                    _output.WriteLine(MessagePackSerializer.ToJson(responseRequestData));
                }
            }

            _output.WriteLine(string.Empty);

            // Return the response
            return response;
        }
    }
}