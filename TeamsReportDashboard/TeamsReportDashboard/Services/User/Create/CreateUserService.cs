using FluentValidation;
using FluentValidation.Results;
using TeamsReportDashboard.Backend.Models.UserDto;
using TeamsReportDashboard.Exceptions;
using TeamsReportDashboard.Interfaces;
using TeamsReportDashboard.Models.Dto;

namespace TeamsReportDashboard.Services.User.Create;

public class CreateUserService : ICreateUserService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateUserDto> _validator;
    private readonly IPasswordService _passwordService;

    public CreateUserService(IUnitOfWork unitOfWork, IValidator<CreateUserDto> validator, IPasswordService passwordService)
    {
        _unitOfWork = unitOfWork;
        _validator = validator;
        _passwordService = passwordService;
    }

    public async Task<CreateUserDto> Execute(CreateUserDto createUserDto)
    {
       await Validate(createUserDto);
       var hashedPassword = _passwordService.HashPassword(createUserDto.Password);
       var user = new Entities.User()
       {
           Email = createUserDto.Email,
           Name = createUserDto.Name,
           Password = hashedPassword,
           Role = createUserDto.Role,
       };
       
       await _unitOfWork.UserRepository.AddAsync(user);
       await _unitOfWork.SaveChangesAsync();
       
       return createUserDto;

    }

    private async Task Validate(CreateUserDto createUserDto)
    {
        var result = await _validator.ValidateAsync(createUserDto);

        var emailExist = await _unitOfWork.UserRepository.GetByEmailAsync(createUserDto.Email);
        
        if(emailExist is not null)
            result.Errors.Add(new FluentValidation.Results.ValidationFailure("Email", "Email is already taken"));
        if (!result.IsValid)
        {
            var errors = result.Errors.Select(failure => failure.ErrorMessage).ToList();

            throw new ErrorOnValidationException(errors);
        }
    }
}