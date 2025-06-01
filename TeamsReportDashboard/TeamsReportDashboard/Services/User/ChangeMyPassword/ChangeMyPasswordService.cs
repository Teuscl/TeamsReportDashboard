using TeamsReportDashboard.Backend.Services.User.ChangeMyPassword;
using TeamsReportDashboard.Exceptions;
using TeamsReportDashboard.Interfaces;
using TeamsReportDashboard.Models.Dto;

namespace TeamsReportDashboard.Services.User.ChangePassword;
 
public class ChangeMyPasswordService : IChangeMyPasswordService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordService _passwordService;
    
    public ChangeMyPasswordService(IUnitOfWork unitOfWork, IPasswordService passwordService)
    {
        _unitOfWork = unitOfWork;
        _passwordService = passwordService;
    }
    public async Task Execute(int userId, ChangeMyPasswordDto changeMyPasswordDto)
    {
        var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
        if (user == null)
            throw new ArgumentException("User not found");
        
        Validate(user, changeMyPasswordDto);
        
        user.Password = _passwordService.HashPassword(changeMyPasswordDto.NewPassword);
        _unitOfWork.UserRepository.Update(user);
        await _unitOfWork.CommitAsync();
    }

    private void Validate(Entities.User user, ChangeMyPasswordDto changeMyPasswordDto)
    {
        var result = new ChangeMyPasswordValidator().Validate(changeMyPasswordDto);
        
        if(!_passwordService.VerifyPassword(changeMyPasswordDto.OldPassword, user.Password))
            result.Errors.Add(new FluentValidation.Results.ValidationFailure("OldPassword", "Old password is incorrect."));
        
        if(!result.IsValid)
            throw new ErrorOnValidationException(result.Errors.Select(e => e.ErrorMessage).ToList());
    }
}