using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace WebApplications.HttpRecorder
{
    /// <summary>
    /// Extension methods.
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// The default parts, used for quick retrieval of defaults.
        /// NOTE: This must remain in numerical order!
        /// </summary>
        private static readonly IReadOnlyList<RequestPart> _defaultParts = new[]
        {
            RequestPart.Version,
            RequestPart.UriScheme,
            RequestPart.UriUserInfo,
            RequestPart.UriHost,
            RequestPart.UriPort,
            RequestPart.UriPath,
            RequestPart.UriQuery,
            RequestPart.Method,
            RequestPart.Headers,
            RequestPart.Content
        };

        /// <summary>
        /// Gets the individual <see cref="RequestPart">parts</see> from a <see cref="RequestParts"/>, in ascending order.
        /// </summary>
        /// <param name="parts">The parts.</param>
        /// <returns></returns>
        public static IEnumerable<RequestPart> GetParts(this RequestParts parts) =>
            parts == RequestParts.Default ? _defaultParts : CalculateParts(parts);

        /// <summary>
        /// Calculates the parts, note this is wrapped by <see cref="GetParts"/> which provides Default parsing.
        /// </summary>
        /// <param name="parts">The parts.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException">parts</exception>
        private static IEnumerable<RequestPart> CalculateParts(RequestParts parts)
        {
            ushort bits = (ushort)parts;

            // Ensure we don't have a bogus value.
            if (bits > (ushort)RequestParts.All)
                throw new ArgumentOutOfRangeException(nameof(parts));

            ushort flag = 1;
            while (flag <= bits)
            {
                if (parts.HasFlag((RequestParts)flag))
                    yield return (RequestPart)flag;

                flag <<= 1;
            }
        }

        /// <summary>
        /// Returns a hash string from a data array.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">request</exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static string GetKeyHash(this byte[] data)
        {
            byte[] hashBytes;
            // Hash stream
            using (MD5 hasher = MD5.Create())
                hashBytes = hasher.ComputeHash(data);
            // NOTE: As base64 encoded hash is always 24 chars, and last two are padding characters ('==') we can ignore them in resulting string.
            char[] hashChars = new char[24];
            Convert.ToBase64CharArray(hashBytes, 0, 16, hashChars, 0);

            return new string(hashChars, 0, 22);
        }
    }
}