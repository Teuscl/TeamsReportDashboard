using TeamsReportDashboard.Backend.Models.UserDto;
using TeamsReportDashboard.Exceptions;
using TeamsReportDashboard.Interfaces;

namespace TeamsReportDashboard.Backend.Services.User.ResetForgottenPassword;

public class ResetForgottenPasswordService : IResetForgottenPasswordService
{
    
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordService _passwordService; // Reutiliza seu serviço de senha

    public ResetForgottenPasswordService(IUnitOfWork unitOfWork, IPasswordService passwordService)
    {
        _unitOfWork = unitOfWork;
        _passwordService = passwordService;
    }
    
    public async Task Execute(ResetForgottenPasswordDto dto)
    {
        
        Validate(dto);
        var user = await _unitOfWork.UserRepository.GetByPasswordResetToken(dto.Token);
        
        // Valida o token
        if (user == null || user.PasswordResetTokenExpiryTime <= DateTime.UtcNow)
        {
            throw new ArgumentException("Token inválido ou expirado.");
        }
        
        user.Password = _passwordService.HashPassword(dto.NewPassword);
        
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpiryTime = null;
        
        _unitOfWork.UserRepository.Update(user);
        await _unitOfWork.SaveChangesAsync();
    }
    
    private void Validate(ResetForgottenPasswordDto dto)
    {
        var result = new ResetForgottenPasswordValidator().Validate(dto);
        if(!result.IsValid)
            throw new ErrorOnValidationException(result.Errors.Select(e => e.ErrorMessage).ToList());
    }
}