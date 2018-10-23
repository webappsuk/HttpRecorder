using System;

namespace WebApplications.HttpRecorder.Logging
{
    /// <summary>
    /// Extension methods to make using <see cref="IRecorderLogger"/> easier.
    /// </summary>
    internal static class Extensions
    {
        /// <summary>
        /// Logs the information.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="message">The message.</param>
        /// <param name="store">The store.</param>
        /// <param name="callerFilePath">The caller file path.</param>
        /// <param name="callerMemberName">Name of the caller member.</param>
        /// <param name="callerLineNumber">The caller line number.</param>
        /// <param name="exception">The exception.</param>
        public static void LogInformation(this IRecorderLogger logger,
            string message,
            string store,
            string callerFilePath,
            string callerMemberName,
            int callerLineNumber,
            Exception exception = null) =>
            logger?.Log(LogLevel.Information,
                $"{message} Store '{store}' for '{callerMemberName}' at {callerFilePath}:line {callerLineNumber}", exception);

        /// <summary>
        /// Logs the error.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="exception">The exception.</param>
        public static void LogError(this IRecorderLogger logger, Exception exception) =>
            logger?.Log(LogLevel.Error, null, exception);

        /// <summary>
        /// Logs the critical.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="exception">The exception.</param>
        public static void LogCritical(this IRecorderLogger logger, Exception exception) =>
            logger?.Log(LogLevel.Critical, null, exception);

    }
}