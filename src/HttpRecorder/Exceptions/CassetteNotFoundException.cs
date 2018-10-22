namespace WebApplications.HttpRecorder.Exceptions
{
    public class CassetteNotFoundException : CassetteException
    {
        internal CassetteNotFoundException(
            string store,
            string callerFilePath,
            string callerMemberName,
            int callerLineNumber)
            : base("No matching recording found!",
                store,
                callerFilePath,
                callerMemberName,
                callerLineNumber)
        {
        }
    }
}