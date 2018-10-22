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
            : base($"{message} In store {store} for '{callerMemberName}' in {callerFilePath}:line {callerLineNumber}" +
                   (innerException is null
                       ? string.Empty
                       : $"{Environment.NewLine}{innerException.Message}"),
                innerException)
        {

        }
    }
}