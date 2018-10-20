using System;

namespace WebApplications.HttpRecorder.Exceptions
{
    public class RecorderException : Exception
    {
        internal RecorderException(string message = null, Exception innerException = null) : base(message, innerException)
        {

        }
    }
}