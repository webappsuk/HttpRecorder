using System;

namespace WebApplications.HttpRecorder.Exceptions
{
    public class CassetteException : Exception
    {
        internal CassetteException(string message,
            string store,
            string callerFilePath,
            string callerMemberName,
            int callerLineNumber,
            Exception innerException = null)
            : base($"{message} Store '{store}' for '{callerMemberName}' at {callerFilePath}:line {callerLineNumber}" +
                   (innerException is null
                       ? string.Empty
                       : $"{Environment.NewLine}{innerException.Message}"),
                innerException)
        {

        }
    }
}