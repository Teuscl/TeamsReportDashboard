using AutoMapper;
using FluentValidation;
using TeamsReportDashboard.Backend.Models.ReportDto;
using TeamsReportDashboard.Backend.Services.User.Update;
using TeamsReportDashboard.Exceptions;
using TeamsReportDashboard.Interfaces;
using TeamsReportDashboard.Models.Dto;
using TeamsReportDashboard.Services.User.Update;
// Removi Microsoft.EntityFrameworkCore pois não é usado diretamente aqui


namespace TeamsReportDashboard.Backend.Services.Report.Update;

public class UpdateReportService : IUpdateReportService // Certifique-se que esta interface existe e o método Execute está nela
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<UpdateReportDto> _validator;
    private readonly IMapper _mapper; 

    // Corrigido: Injetar IMapper, não a classe concreta Mapper
    public UpdateReportService(IUnitOfWork unitOfWork, IValidator<UpdateReportDto> validator, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _validator = validator;
        _mapper = mapper;
    }

    public async Task Execute(int id, UpdateReportDto updateReportDto)
    {
        
        var report = await Validate(id, updateReportDto);

        if (report.RequesterId != updateReportDto.RequesterId)
        {
            var newRequester = await _unitOfWork.RequesterRepository.GetRequesterAsync(updateReportDto.RequesterId);

            if (newRequester != null)
            {
                report.Requester = newRequester;
            }
        }
        
        // 2. Mapear o DTO para a entidade existente.
        // O AutoMapper atualizará 'report' com os valores não nulos de 'updateReportDto'.
        // _mapper.Map é síncrono quando mapeia para um objeto existente.
        _mapper.Map(updateReportDto, report);
        report.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.ReportRepository.UpdateReport(report);
        await _unitOfWork.CommitAsync(); // Ou _unitOfWork.CommitAsync(); dependendo da sua implementação
    }

    // Renomeado para maior clareza e corrigido para retornar o Report
    private async Task<Entities.Report> Validate(int id, UpdateReportDto updateReportDto)
    {
        // Valida o DTO primeiro
        var validationResult = await _validator.ValidateAsync(updateReportDto);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(error => error.ErrorMessage).ToList();
            throw new ErrorOnValidationException(errors);
        }

        // Busca o relatório
        var report = await _unitOfWork.ReportRepository.GetReportAsync(id);
        if (report == null)
        {
            throw new ErrorOnValidationException(new List<string> { "Report not found" });
        }

        return report; // Retorna o relatório se tudo estiver OK
    }

    
}