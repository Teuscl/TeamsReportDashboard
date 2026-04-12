using System.Security.Cryptography;
using System.Text;
using FluentValidation;
using TeamsReportDashboard.Backend.Models.UserDto;
using TeamsReportDashboard.Exceptions;
using TeamsReportDashboard.Interfaces;

namespace TeamsReportDashboard.Backend.Services.User.ResetForgottenPassword;

public class ResetForgottenPasswordService : IResetForgottenPasswordService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordService _passwordService;
    private readonly IValidator<ResetForgottenPasswordDto> _validator;

    public ResetForgottenPasswordService(IUnitOfWork unitOfWork, IPasswordService passwordService, IValidator<ResetForgottenPasswordDto> validator)
    {
        _unitOfWork = unitOfWork;
        _passwordService = passwordService;
        _validator = validator;
    }
    
    public async Task Execute(ResetForgottenPasswordDto dto)
    {
        
        Validate(dto);
        var hashedToken = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(dto.Token)));
        var user = await _unitOfWork.UserRepository.GetByPasswordResetToken(hashedToken);
        
        if (user == null || user.PasswordResetTokenExpiryTime <= DateTime.UtcNow)
            throw new ErrorOnValidationException(["Token inválido ou expirado."]);

        user.Password = _passwordService.HashPassword(dto.NewPassword);

        user.PasswordResetToken = null;
        user.PasswordResetTokenExpiryTime = null;

        // Invalida sessões existentes após troca de senha
        user.RefreshToken = null;
        user.RefreshTokenExpiryTime = null;
        
        _unitOfWork.UserRepository.Update(user);
        await _unitOfWork.SaveChangesAsync();
    }
    
    private void Validate(ResetForgottenPasswordDto dto)
    {
        var result = _validator.Validate(dto);
        if(!result.IsValid)
            throw new ErrorOnValidationException(result.Errors.Select(e => e.ErrorMessage).ToList());
    }
}