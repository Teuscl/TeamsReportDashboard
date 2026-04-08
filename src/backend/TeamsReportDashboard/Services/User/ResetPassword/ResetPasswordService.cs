using FluentValidation;
using TeamsReportDashboard.Backend.Models.UserDto;
using TeamsReportDashboard.Exceptions;
using TeamsReportDashboard.Interfaces;
using TeamsReportDashboard.Models.Dto;

namespace TeamsReportDashboard.Backend.Services.User.ResetPassword;

public class ResetPasswordService : IResetPasswordService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordService _passwordService;
    private readonly IValidator<ResetPasswordDto> _validator;

    public ResetPasswordService(IUnitOfWork unitOfWork, IPasswordService passwordService, IValidator<ResetPasswordDto> validator)
    {
        _unitOfWork = unitOfWork;
        _passwordService = passwordService;
        _validator = validator;
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
        var result = _validator.Validate(resetPasswordDto);
        if(!result.IsValid)
            throw new ErrorOnValidationException(result.Errors.Select(e => e.ErrorMessage).ToList());
    }
}