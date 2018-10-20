using MessagePack;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using WebApplications.HttpRecorder.Serialization;
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
        public void EncodeAll()
        {
            string hash;
            byte[] data;
            using (HttpRequestMessage request =
                new HttpRequestMessage(HttpMethod.Get, "https://u:p@localhost:1234/a/b/c?a=1"))
            {
                using (ByteArrayContent content = new ByteArrayContent(new byte[] { 1, 2, 3, 4 }))
                {
                    request.Content = content;
                    request.Headers.Add("Test", "a");
                    request.Headers.Add("test", "b");

                    // Serialize data
                    data = MessagePackSerializer.Serialize(request, RecorderResolver.Instance);
                }


                // Get the hash key
                hash = data.GetKeyHash();

                string json = MessagePackSerializer.ToJson(data);
                _output.WriteLine($"Serialized to {data.Length} bytes (JSON length {json.Length}), Hash: {hash}");
                _output.WriteLine(json);



                //Assert.Equal("8eh1pQYr1nOioHTxdvDXAw", hash);
                //Assert.Equal(51, data.Length);

                using (HttpRequestMessage newRequest =
                    MessagePackSerializer.Deserialize<HttpRequestMessage>(data, RecorderResolver.Instance))
                {
                    // Check for that singletons are being used.
                    Assert.Same(HttpMethod.Get, newRequest.Method);
                    Assert.Equal(request.RequestUri.ToString(), newRequest.RequestUri.ToString());

                    // Re-serialize
                    byte[] data2 = MessagePackSerializer.Serialize(newRequest, RecorderResolver.Instance);
                    Assert.Equal(data, data2);
                    Assert.Equal(json, MessagePackSerializer.ToJson(data2));
                }
            }
        }

        [Fact]
        public async Task TestStreams()
        {
            using (Cassette cassette = new Cassette())
            using (HttpClient client = cassette.GetClient())
            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "https://jsonplaceholder.typicode.com/posts"))
            using (MemoryStream memoryStream = new MemoryStream())
            {
                // Write a JSON object to stream.
                byte[] bodyData = Encoding.UTF8.GetBytes(@"{
                    title: 'foo',
                    body: 'bar',
                    userId: 1
                    }");

                memoryStream.Write(bodyData);

                long eos = memoryStream.Position;

                // Reset memory stream ready for reading
                memoryStream.Seek(0, SeekOrigin.Begin);

                // Create a request with a body stream
                using (StreamContent contentStream = new StreamContent(memoryStream))
                {
                    request.Content = contentStream;
                    Assert.Equal(0, memoryStream.Position);
                    using (HttpResponseMessage response = await client.SendAsync(request))
                    {
                        // We must have moved memory stream to end at this point
                        Assert.Equal(eos, memoryStream.Position);

                        string content = await response.Content.ReadAsStringAsync();
                        _output.WriteLine(content);
                    }
                }
            }
        }
    }
}
