using System.Security.Cryptography;
using System.Web;
using TeamsReportDashboard.Backend.Models.UserDto;
using TeamsReportDashboard.Exceptions;
using TeamsReportDashboard.Interfaces;

namespace TeamsReportDashboard.Backend.Services.User.ForgotPassword;

public class ForgotPasswordService : IForgotPasswordService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;
    
    public ForgotPasswordService(IUnitOfWork unitOfWork, IEmailService emailService)
    {
        _unitOfWork = unitOfWork;
        _emailService = emailService;
    }
    
    public async Task Execute(ForgotPasswordDto forgotPasswordDto)
    {
        
        Validate(forgotPasswordDto);
        var user = await _unitOfWork.UserRepository.GetByEmailAsync(forgotPasswordDto.Email);
        
        if (user == null)
            return;
        
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        user.PasswordResetToken = token;
        user.PasswordResetTokenExpiryTime = DateTime.UtcNow.AddMinutes(30);
        
        _unitOfWork.UserRepository.Update(user);
        await _unitOfWork.SaveChangesAsync();
        
        // 2. Monta o link de redefinição
        // A URL base do frontend deve vir de uma configuração (ex: appsettings.json)
        
        var resetLink = $"http://localhost:5173/reset-password?token={HttpUtility.UrlEncode(token)}"; // Exemplo com porta do Vite

        // 3. Envia o email
        await _emailService.SendPasswordResetEmailAsync(user.Email, user.Name, resetLink);
        
        
    }
    private void Validate(ForgotPasswordDto dto)
    {
        var result = new ForgotPasswordValidator().Validate(dto);
        if(!result.IsValid)
            throw new ErrorOnValidationException(result.Errors.Select(e => e.ErrorMessage).ToList());
    }
    
}