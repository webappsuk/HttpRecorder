using MessagePack;
using MessagePack.Formatters;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;

namespace WebApplications.HttpRecorder.Serialization
{
    /// <summary>
    /// Used to serialize cassettes
    /// </summary>
    /// <seealso cref="MessagePack.IFormatterResolver" />
    internal sealed class RecorderResolver : IFormatterResolver
    {
        /// <summary>
        /// The singleton.
        /// </summary>
        public static readonly IFormatterResolver Instance = new RecorderResolver();

        private static readonly ConcurrentDictionary<RequestParts, RecorderResolver> _requestPartResolvers
            = new ConcurrentDictionary<RequestParts, RecorderResolver>();

        /// <summary>
        /// The formatter default formatter map.
        /// </summary>
        private static readonly IReadOnlyDictionary<Type, object> _formatterMap = new Dictionary<Type, object>()
        {
            {typeof(HttpRequestMessage), RequestFormatter.Instance},
            {typeof(HttpResponseMessage), ResponseFormatter.Instance},
            {typeof(HttpMethod), HttpMethodFormatter.Instance}
        };

        private readonly IMessagePackFormatter<HttpRequestMessage> _customFormatter;

        /// <summary>
        /// Prevents a default instance of the <see cref="RecorderResolver"/> class from being created.
        /// </summary>
        private RecorderResolver(IMessagePackFormatter<HttpRequestMessage> customFormatter = null)
            => _customFormatter = customFormatter;

        /// <summary>
        /// Gets a resolver that can be used to partial serialize requests for key generation.
        /// </summary>
        /// <param name="requestParts">The request parts.</param>
        /// <returns></returns>
        public static IFormatterResolver Get(RequestParts requestParts)
        {
            if (requestParts == RequestParts.Default || requestParts == RequestParts.All)
                return Instance;

            return _requestPartResolvers.GetOrAdd(
                requestParts,
                rp => new RecorderResolver(new KeyFormatter(requestParts)));
        }

        /// <inheritdoc />
        public IMessagePackFormatter<T> GetFormatter<T>() =>
            _customFormatter != null && typeof(T) == typeof(HttpRequestMessage)
                ? (IMessagePackFormatter<T>)_customFormatter
                : FormatterCache<T>.Formatter;


        /// <summary>
        /// Holds static cache of formatters for rapid lookup
        /// </summary>
        /// <typeparam name="T"></typeparam>
        private static class FormatterCache<T>
        {
            /// <summary>
            /// The formatter for <typeparamref name="T"/>.
            /// </summary>
            public static readonly IMessagePackFormatter<T> Formatter;


            /// <summary>
            /// Initializes the <see cref="FormatterCache{T}"/> class.
            /// </summary>
            static FormatterCache() => Formatter = (IMessagePackFormatter<T>)(_formatterMap.TryGetValue(typeof(T), out object value) ? value : null);
        }
    }
}