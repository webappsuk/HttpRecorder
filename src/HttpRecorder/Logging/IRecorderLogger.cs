using System;

namespace WebApplications.HttpRecorder.Logging
{
    /// <summary>
    /// Used by HttpRecorder to expose logs.
    /// </summary>
    public interface IRecorderLogger
    {
        /// <summary>
        /// Logs the message or exception at the specified level.
        /// </summary>
        /// <param name="level">The level.</param>
        /// <param name="message">The message.</param>
        /// <param name="exception">The exception.</param>
        void Log(LogLevel level, string message = null, Exception exception = null);
    }
}