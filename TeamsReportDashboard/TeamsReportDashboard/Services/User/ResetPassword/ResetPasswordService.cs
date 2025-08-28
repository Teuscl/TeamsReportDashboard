using TeamsReportDashboard.Backend.Models.UserDto;
using TeamsReportDashboard.Backend.Services.User.ChangeMyPassword;
using TeamsReportDashboard.Exceptions;
using TeamsReportDashboard.Interfaces;
using TeamsReportDashboard.Models.Dto;

namespace TeamsReportDashboard.Backend.Services.User.ResetPassword;
 
public class ResetPasswordService : IResetPasswordService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordService _passwordService;
    
    public ResetPasswordService(IUnitOfWork unitOfWork, IPasswordService passwordService)
    {
        _unitOfWork = unitOfWork;
        _passwordService = passwordService;
    }
    public async Task Execute(int userId, ResetPasswordDto resetPasswordDto)
    {
        var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
        if (user == null)
            throw new ArgumentException("User not found");
        
        Validate(user, resetPasswordDto);
        
        user.Password = _passwordService.HashPassword(resetPasswordDto.NewPassword);
        Console.WriteLine($"Resetando a senha do usuario {user.Name}");
        _unitOfWork.UserRepository.Update(user);
        await _unitOfWork.SaveChangesAsync();
    }

    private void Validate(TeamsReportDashboard.Entities.User user, ResetPasswordDto resetPasswordDto)
    {
        var result = new ResetPasswordValidator().Validate(resetPasswordDto);
        if(!result.IsValid)
            throw new ErrorOnValidationException(result.Errors.Select(e => e.ErrorMessage).ToList());
    }
}