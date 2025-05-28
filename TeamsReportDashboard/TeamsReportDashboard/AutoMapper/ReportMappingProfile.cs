using AutoMapper;
using TeamsReportDashboard.Backend.Entities;
using TeamsReportDashboard.Backend.Models.ReportDto;

namespace TeamsReportDashboard.Backend.AutoMapper;

public class ReportMappingProfile : Profile
{
    public ReportMappingProfile()
    {
        CreateMap<UpdateReportDto, Report>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
    }
}