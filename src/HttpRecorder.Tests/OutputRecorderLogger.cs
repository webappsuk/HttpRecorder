using System;
using System.Collections.Generic;
using WebApplications.HttpRecorder.Logging;
using Xunit.Abstractions;

namespace WebApplications.HttpRecorder.Tests
{
    internal class OutputRecorderLogger : RecorderLoggerBase
    {
        private static readonly string _dashes = new string('_', 80);
        private readonly ITestOutputHelper _output;

        public OutputRecorderLogger(ITestOutputHelper output)
        {
            _output = output;
        }

        /// <inheritdoc />
        public override void Log(LogLevel level, string message = null, Exception exception = null)
        {
            _output.WriteLine(ToString(level, message, exception));

            if (exception != null)
                _output.WriteLine(_dashes);

            HashSet<Exception> exceptions = new HashSet<Exception>();
            while (!(exception is null) && !exceptions.Contains(exception))
            {
                _output.WriteLine($"{exception.GetType()} : {exception.Message}");
                if (!string.IsNullOrWhiteSpace(exception.StackTrace))
                    _output.WriteLine(exception.StackTrace);
                _output.WriteLine(_dashes);
                exceptions.Add(exception);
                exception = exception.InnerException;
            }
        }
    }
}