using TeamsReportDashboard.Backend.Models.UserDto;

namespace TeamsReportDashboard.Backend.Services.User.UpdateMyProfile;

public interface IUpdateMyProfileService{
    Task Execute(int userId, UpdateMyProfileDto dto);
}