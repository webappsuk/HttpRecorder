using System;

namespace WebApplications.HttpRecorder.Exceptions
{
    public class DeserializeRequestException : SerializerException
    {
        internal DeserializeRequestException(string message = null, Exception innerException = null) : base(message, innerException)
        {
        }
    }
}