using TeamsReportDashboard.Backend.Models.Requester.BulkInsert;

namespace TeamsReportDashboard.Backend.Services.Requester.BulkCreate;

public interface IBulkCreateRequesterService
{
    Task<BulkInsertResultDto> Execute(IFormFile file);
}