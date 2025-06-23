
using System.Net;
using TeamsReportDashboard.Exceptions;

namespace TeamsReportDashboard.Backend.Exceptions
{
    // Esta exceção herda da sua classe base
    public class ConflictException : ExceptionBase
    {
        public ConflictException(string message) : base(message) { }

        public override HttpStatusCode GetStatusCode()
        {
            return HttpStatusCode.Conflict; // 409
        }

        public override List<string> GetErrorMessages()
        {
            return new List<string> { Message };
        }
    }
}