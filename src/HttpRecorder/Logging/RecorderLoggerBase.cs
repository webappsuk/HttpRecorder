using System;
using System.Text;

namespace WebApplications.HttpRecorder.Logging
{
    /// <summary>
    /// Base class for implementing <see cref="IRecorderLogger"/>
    /// </summary>
    /// <seealso cref="IRecorderLogger" />
    public abstract class RecorderLoggerBase : IRecorderLogger
    {
        public abstract void Log(LogLevel level, string message = null, Exception exception = null);

        protected string ToString(LogLevel level, string message = null, Exception exception = null)
        {
            StringBuilder builder = new StringBuilder(level.ToString());
            builder.Append(": ");
            if (string.IsNullOrWhiteSpace(message))
                builder.Append(exception is null ? "Empty log message!" : exception.Message);
            else
            {
                builder.Append(message);
                if (exception != null)
                {
                    builder.AppendLine();
                    builder.Append(exception.Message);
                }
            }

            return builder.ToString();
        }
    }
}