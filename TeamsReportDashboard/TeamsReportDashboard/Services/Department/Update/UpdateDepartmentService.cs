using FluentValidation;
using TeamsReportDashboard.Backend.Models.DepartmentDto;
using TeamsReportDashboard.Exceptions;
using TeamsReportDashboard.Interfaces;

namespace TeamsReportDashboard.Backend.Services.Department.Update;

public class UpdateDepartmentService : IUpdateDepartmentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<UpdateDepartmentDto> _validator;

    public UpdateDepartmentService(IUnitOfWork unitOfWork, IValidator<UpdateDepartmentDto> validator)
    {
        _unitOfWork = unitOfWork;
        _validator = validator;
    }

    public async Task Execute(int id, UpdateDepartmentDto departmentDto)
    {
        // 1. Validar os dados de entrada
        await Validate(departmentDto);

        // 2. Buscar o departamento existente no banco
        var department = await _unitOfWork.DepartmentRepository.GetDepartmentAsync(id);
        if (department == null)
        {
            // Lançar uma exceção se não encontrar
            throw new KeyNotFoundException($"Departamento com ID {id} não encontrado.");
        }

        // 3. Atualizar as propriedades
        department.Name = departmentDto.Name;
        department.UpdatedAt = DateTime.Now;

        _unitOfWork.DepartmentRepository.UpdateDepartment(department);
        // 4. Salvar as mudanças no banco de dados
        await _unitOfWork.CommitAsync();
    }
    
    private async Task Validate(UpdateDepartmentDto updateDepartmentDto)
    {
        var result = await _validator.ValidateAsync(updateDepartmentDto);
        if (!result.IsValid)
        {
            var errors = result.Errors.Select(failure => failure.ErrorMessage).ToList();
            throw new ErrorOnValidationException(errors);
        }
    }
}