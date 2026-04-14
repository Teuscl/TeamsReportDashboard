using TeamsReportDashboard.Backend.Models.UserDto;

namespace TeamsReportDashboard.Backend.Services.User.UpdateMyProfile;

public interface IUpdateMyProfileService{
    Task Execute(Guid userId, UpdateMyProfileDto dto);
}