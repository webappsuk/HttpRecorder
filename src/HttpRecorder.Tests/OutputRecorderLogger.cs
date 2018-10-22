using System;
using WebApplications.HttpRecorder.Logging;
using Xunit.Abstractions;

namespace WebApplications.HttpRecorder.Tests
{
    public class OutputRecorderLogger : RecorderLoggerBase
    {
        private readonly ITestOutputHelper _output;

        public OutputRecorderLogger(ITestOutputHelper output)
        {
            _output = output;
        }

        /// <inheritdoc />
        public override void Log(LogLevel level, string message = null, Exception exception = null) =>
            _output.WriteLine(base.ToString(level, message, exception));
    }
}