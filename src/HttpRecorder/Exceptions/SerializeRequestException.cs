using System;

namespace WebApplications.HttpRecorder.Exceptions
{
    public class SerializeRequestException : SerializerException
    {
        internal SerializeRequestException(string message = null, Exception innerException = null) : base(message, innerException)
        {
        }
    }
}