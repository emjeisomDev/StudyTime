using AutoMapper;
using StudyTime.Application.Dtos;
using StudyTime.Api.Contracts.Responses;

namespace StudyTime.Api.Mappings;

public sealed class ApiMappingProfile : Profile
{
    public ApiMappingProfile()
    {
        CreateMap<StudyAreaDto, StudyAreaResponse>();
        CreateMap<StudyPlanDto, StudyPlanResponse>();
        CreateMap<StudyAreaWeekDto, StudyAreaWeekResponse>();
        CreateMap<StudyRecordDto, StudyRecordResponse>();
        CreateMap<WeeklyAssessmentDto, WeeklyAssessmentResponse>();
        CreateMap<PreviousWeekSummaryItemDto, PreviousWeekSummaryItemResponse>();
        CreateMap<PreviousWeekSummaryDto, PreviousWeekSummaryResponse>();
    }
}
