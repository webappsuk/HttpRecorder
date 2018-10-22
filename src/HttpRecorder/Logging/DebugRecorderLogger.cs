using System;
using System.Diagnostics;

namespace WebApplications.HttpRecorder.Logging
{
    public class DebugRecorderLogger : RecorderLoggerBase
    {
        public override void Log(LogLevel level, string message = null, Exception exception = null)
        {
            Debug.WriteLine(ToString(level, message, exception));
        }
    }
}