namespace TeamsReportDashboard.Backend.Models.Requester.BulkInsert;

public class BulkInsertFailure
{
    public int RowNumber { get; set; }
    public string ErrorMessage { get; set; }
    public string OffendingLine { get; set; }
}