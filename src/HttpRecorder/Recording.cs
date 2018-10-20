using System.Net.Http;

namespace WebApplications.HttpRecorder
{
    public class Recording
    {
        public string Hash { get; }

        public RecordingOptions Options { get; }

        private readonly byte[] _requestData;
        private readonly byte[] _responseData;

        internal Recording(string hash, RecordingOptions options)
        {
            Hash = hash;
            Options = options;
        }

        public HttpResponseMessage GetResponse()
        {
            throw new System.NotImplementedException();
        }

        public HttpRequestMessage GetRequest()
        {
            throw new System.NotImplementedException();
        }
    }
}