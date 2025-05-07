using TeamsReportDashboard.Models.Dto;

namespace TeamsReportDashboard.Services.User.Create;

public interface ICreateUserService
{
     Task<CreateUserDto> Execute(CreateUserDto createUserDto);
}