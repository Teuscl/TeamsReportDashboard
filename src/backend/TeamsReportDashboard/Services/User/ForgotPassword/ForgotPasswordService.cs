using System.Security.Cryptography;
using System.Text;
using System.Web;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using TeamsReportDashboard.Backend.Models.UserDto;
using TeamsReportDashboard.Exceptions;
using TeamsReportDashboard.Interfaces;

namespace TeamsReportDashboard.Backend.Services.User.ForgotPassword;

public class ForgotPasswordService : IForgotPasswordService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly IValidator<ForgotPasswordDto> _validator;

    public ForgotPasswordService(IUnitOfWork unitOfWork, IEmailService emailService, IConfiguration configuration, IValidator<ForgotPasswordDto> validator)
    {
        _unitOfWork = unitOfWork;
        _emailService = emailService;
        _configuration = configuration;
        _validator = validator;
    }

    public async Task Execute(ForgotPasswordDto forgotPasswordDto)
    {
        Validate(forgotPasswordDto);
        var user = await _unitOfWork.UserRepository.GetByEmailAsync(forgotPasswordDto.Email);

        if (user == null)
            return;

        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        user.PasswordResetToken = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
        user.PasswordResetTokenExpiryTime = DateTime.UtcNow.AddMinutes(30);

        _unitOfWork.UserRepository.Update(user);
        await _unitOfWork.SaveChangesAsync();

        var frontendUrl = _configuration["FrontendUrl"] ?? "http://localhost:60414";
        var resetLink = $"{frontendUrl}/reset-password?token={HttpUtility.UrlEncode(rawToken)}";

        await _emailService.SendPasswordResetEmailAsync(user.Email, user.Name, resetLink);
    }

    private void Validate(ForgotPasswordDto dto)
    {
        var result = _validator.Validate(dto);
        if (!result.IsValid)
            throw new ErrorOnValidationException(result.Errors.Select(e => e.ErrorMessage).ToList());
    }
}