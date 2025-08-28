namespace TeamsReportDashboard.Backend.Models.Requester.BulkInsert;

public class BulkInsertResultDto
{
    public int SuccessfulInserts { get; set; }
    public List<BulkInsertFailure> Failures { get; set; } = new List<BulkInsertFailure>();
    public bool HasErrors => Failures.Any();
}