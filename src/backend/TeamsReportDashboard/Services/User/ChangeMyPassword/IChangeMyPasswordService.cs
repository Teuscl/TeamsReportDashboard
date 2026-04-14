using TeamsReportDashboard.Models.Dto;

namespace TeamsReportDashboard.Backend.Services.User.ChangeMyPassword;

public interface IChangeMyPasswordService
{
    public Task Execute(Guid userId, ChangeMyPasswordDto changeMyPasswordDto);
}