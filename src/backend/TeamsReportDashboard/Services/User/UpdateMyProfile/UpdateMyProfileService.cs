using FluentValidation;
using TeamsReportDashboard.Backend.Models.UserDto;                                                                                                                           
using TeamsReportDashboard.Exceptions;
using TeamsReportDashboard.Interfaces; 

namespace TeamsReportDashboard.Backend.Services.User.UpdateMyProfile;


public class UpdateMyProfileService : IUpdateMyProfileService{

    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<UpdateMyProfileDto> _validator;

    public UpdateMyProfileService(IUnitOfWork unitOfWork, IValidator<UpdateMyProfileDto> validator){
        _unitOfWork = unitOfWork;
        _validator = validator;
    }

    public async Task Execute(Guid userId, UpdateMyProfileDto dto){
        var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
        if (user == null)
            throw new ErrorOnValidationException(new List<string> { "User not found" });

        var result = await _validator.ValidateAsync(dto);
        
        if(dto.Email != user.Email){
            var emailTaken = await _unitOfWork.UserRepository.ExistsWithEmailAsync(dto.Email, userId);
            if (emailTaken)
                result.Errors.Add(new FluentValidation.Results.ValidationFailure("Email", "Email already taken"));
        }

        if(!result.IsValid){
            throw new ErrorOnValidationException(result.Errors.Select(x => x.ErrorMessage).ToList());
        }

        user.Name = dto.Name;
        user.Email = dto.Email;

        _unitOfWork.UserRepository.Update(user);
        await _unitOfWork.SaveChangesAsync();
    }
}