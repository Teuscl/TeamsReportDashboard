using CsvHelper.Configuration;

namespace TeamsReportDashboard.Backend.Models.Requester.BulkInsert;

public sealed class RequesterCsvMap : ClassMap<RequesterCsvRecord>
{
    public RequesterCsvMap()
    {
        Map(m => m.Name).Name("Nome");
        Map(m => m.Department).Name("Departamento");
        Map(m => m.Email).Name("E-mail");
    }
}