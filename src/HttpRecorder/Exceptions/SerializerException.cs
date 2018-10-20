using System;

namespace WebApplications.HttpRecorder.Exceptions
{
    public class SerializerException : RecorderException
    {
        internal SerializerException(string message = null, Exception innerException = null) : base(message, innerException)
        {

        }
    }
}