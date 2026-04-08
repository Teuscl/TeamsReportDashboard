using System.Net;

namespace TeamsReportDashboard.Exceptions;

public abstract class ExceptionBase : System.Exception
{
    protected ExceptionBase(string message) : base(message) { }
    
    public abstract IList<string> GetErrorMessages();
    public abstract HttpStatusCode GetStatusCode();
}