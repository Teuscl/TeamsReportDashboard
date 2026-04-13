using FluentValidation;
using TeamsReportDashboard.Backend.Models.UserDto;

namespace TeamsReportDashboard.Backend.Services.User.UpdateMyProfile;

public class UpdateMyProfileValidator : AbstractValidator<UpdateMyProfileDto>{
    public UpdateMyProfileValidator(){
        RuleFor(x => x.Name).NotEmpty().WithMessage("O nome é obrigatório.");
        RuleFor(x => x.Email).NotEmpty().EmailAddress().WithMessage("O email é inválido.");
    }
}

