using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using WebApplications.HttpRecorder.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace WebApplications.HttpRecorder.Tests
{
    public class SerializerTests
    {
        private readonly ITestOutputHelper _output;

        public SerializerTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task TestPostStringContent()
        {
            const string testString = "Test String";

            using (TestMemoryStore store = new TestMemoryStore())
            using (Cassette cassette = new Cassette(
                store,
                CassetteOptions.RecordedRequests & CassetteOptions.UseRecordedRequest,
                logger: new OutputRecorderLogger(_output)))
            using (TestHttpMessageHandler testHandler = new TestHttpMessageHandler(_output))
            {
                HttpRequestMessage request;
                StringContent content;
                HttpResponseMessage response;
                using (HttpMessageHandler handler =
                    cassette.GetHttpMessageHandler(testHandler, CassetteOptions.Overwrite))
                using (HttpClient client = new HttpClient(handler))
                using (request = new HttpRequestMessage(HttpMethod.Post, "https://test.handler/"))
                using (content = new StringContent(testString,
                    // Attempt non-standard encoding (web normally uses UTF-8)
                    Encoding.Unicode))
                {
                    request.Content = content;

                    // Store should not have been accessed yet
                    Assert.Equal(0, store.GetAsyncCount);
                    Assert.Equal(0, store.StoreAsyncCount);

                    using (response = await client.SendAsync(request))
                    {
                        // Ensure test handler was called
                        Assert.Equal(1, testHandler.CallCount);
                        Assert.NotNull(response.Content);
                        Assert.IsType<StreamContent>(response.Content);

                        Assert.Equal(testString, await response.Content.ReadAsStringAsync());

                        // As we're in overwrite mode we should not have queries the store.
                        Assert.Equal(0, store.GetAsyncCount);
                        Assert.Equal(1, store.StoreAsyncCount);

                        // Should only be on item in store.
                        Assert.Single(store);
                    }
                }

                // This time repeat the post in playback mode, which will error if not recorded properly above
                using (HttpClient client = cassette.GetClient(CassetteOptions.Playback))
                using (HttpRequestMessage request2 = new HttpRequestMessage(HttpMethod.Post, "https://test.handler/"))
                using (StringContent content2 = new StringContent(testString,
                    // Attempt non-standard encoding (web normally uses UTF-8)
                    Encoding.Unicode))
                {
                    request2.Content = content2;
                    using (HttpResponseMessage response2 = await client.SendAsync(request2))
                    {
                        // Ensure we didn't call message handler
                        Assert.Equal(1, testHandler.CallCount);
                        Assert.NotNull(response2.Content);
                        Assert.IsType<StreamContent>(response2.Content);
                        Assert.Equal(testString, await response2.Content.ReadAsStringAsync());

                        // Ensure nothing was re-used
                        Assert.NotSame(request, request2);
                        Assert.NotSame(request, response2.RequestMessage);
                        Assert.NotSame(response, response2);

                        // One gets but only one set asked of store
                        Assert.Equal(1, store.GetAsyncCount);
                        Assert.Equal(1, store.StoreAsyncCount);


                        // Should only be on item in store.
                        Assert.Single(store);
                    }
                }
            }
        }
    }
}
