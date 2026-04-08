using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using FluentValidation;
using TeamsReportDashboard.Backend.Models.Requester;
using TeamsReportDashboard.Backend.Models.Requester.BulkInsert;
using TeamsReportDashboard.Exceptions;
using TeamsReportDashboard.Interfaces;

namespace TeamsReportDashboard.Backend.Services.Requester.BulkCreate;

public class BulkCreateRequesterService : IBulkCreateRequesterService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateRequesterDto> _validator;

    public BulkCreateRequesterService(IUnitOfWork unitOfWork, IValidator<CreateRequesterDto> validator)
    {
        _unitOfWork = unitOfWork;
        _validator = validator;
    }
    
    public async Task<BulkInsertResultDto> Execute(IFormFile file)
    {
        var result = new BulkInsertResultDto();
        var records = new List<RequesterCsvRecord>();

        try
        {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = ";",
                HasHeaderRecord = true,
                Encoding = Encoding.UTF8,
                MissingFieldFound = null,
                HeaderValidated = null,
            };

            using var reader = new StreamReader(file.OpenReadStream());
            using var csv = new CsvReader(reader, config);

            csv.Context.RegisterClassMap<RequesterCsvMap>();

            records = csv.GetRecords<RequesterCsvRecord>().ToList();
        }
        catch (Exception ex)
        {
            throw new ErrorOnValidationException(new List<string> {$"Fail to read CSV file: {ex.Message} "});
        }

        if (!records.Any())
        {
            result.Failures.Add(new BulkInsertFailure { RowNumber = 0, ErrorMessage = "Invalid csv file" });
        }
        
        // 2. Otimização: buscar dados existentes do banco de uma vez
        var existingEmails = (await _unitOfWork.RequesterRepository.GetAllAsync())
                                .Select(r => r.Email.ToLowerInvariant())
                                .ToHashSet();

        var allDepartments = (await _unitOfWork.DepartmentRepository.GetAllAsync())
                                .ToDictionary(d => d.Name.ToLowerInvariant(), d => d.Id);

        var newRequesters = new List<Entities.Requester>();
        
        // 3. Validando cada linha
        for (int i = 0; i < records.Count; i++)
        {
            var record = records[i];
            var rowNumber = i + 2; // +1 do índice 0 e +1 do cabeçalho
            
            // Validação de Departamento
            if (!allDepartments.TryGetValue(record.Department.ToLowerInvariant(), out var departmentId))
            {
                result.Failures.Add(new BulkInsertFailure { RowNumber = rowNumber, ErrorMessage = $"Departamento '{record.Department}' não encontrado.", OffendingLine = $"{record.Name};{record.Email}" });
                continue;
            }

            // Mapeando para o DTO de criação para reutilizar a validação
            var createDto = new CreateRequesterDto
            {
                Name = record.Name,
                Email = record.Email,
                DepartmentId = departmentId
            };

            // Reutilizando o FluentValidation
            var validationResult = await _validator.ValidateAsync(createDto);
            if (!validationResult.IsValid)
            {
                result.Failures.Add(new BulkInsertFailure { RowNumber = rowNumber, ErrorMessage = validationResult.Errors.First().ErrorMessage, OffendingLine = $"{record.Name};{record.Email}" });
                continue;
            }
            
            // Verificando duplicidade de e-mail (tanto no banco quanto no próprio arquivo)
            if (existingEmails.Contains(createDto.Email.ToLowerInvariant()))
            {
                result.Failures.Add(new BulkInsertFailure { RowNumber = rowNumber, ErrorMessage = $"E-mail '{createDto.Email}' já existe no banco de dados.", OffendingLine = $"{record.Name};{record.Email}" });
                continue;
            }

            // Adiciona o e-mail à lista de checagem para evitar duplicatas no mesmo arquivo
            existingEmails.Add(createDto.Email.ToLowerInvariant());

            // Se passou em todas as validações, prepara a entidade para inserção
            newRequesters.Add(new Entities.Requester
            {
                Name = createDto.Name,
                Email = createDto.Email,
                DepartmentId = createDto.DepartmentId,
                CreatedAt = DateTime.UtcNow
            });
        }

        // 4. Inserindo no banco de dados (apenas se não houver falhas)
        if (result.HasErrors)
        {
            return result; // Retorna o relatório de erros, nada foi salvo
        }

        try
        {
            await _unitOfWork.BeginTransactionAsync();
            
            await _unitOfWork.RequesterRepository.CreateRequesterRangeAsync(newRequesters); // Um método otimizado para inserção em lote seria ideal
            await _unitOfWork.CommitAsync();

            result.SuccessfulInserts = newRequesters.Count;
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackAsync();
            // Limpa a lista de sucesso e adiciona um erro genérico
            result.SuccessfulInserts = 0;
            result.Failures.Clear();
            result.Failures.Add(new BulkInsertFailure { RowNumber = 0, ErrorMessage = $"Ocorreu um erro ao salvar os dados no banco: {ex.Message}" });
        }

        return result;
    }
}