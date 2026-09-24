using AutoMapper;

using StudyTime.Application.Dtos;
using StudyTime.Domain.Entities;

namespace StudyTime.Application.Mappings;

/// <summary>
/// Fonte composta para projeção: agrega as três entidades que compõem
/// uma configuração semanal (StudyAreaWeek + StudyAreaWeekAssessment +
/// WeeklyAssessment) para produzir um StudyAreaWeekDto.
/// As entidades de domínio não possuem navigation properties; por isso
/// os três objetos são passados explicitamente pelo handler.
/// </summary>
public sealed record StudyAreaWeekMappingSource(
    StudyAreaWeek Week,
    StudyAreaWeekAssessment Assessment,
    WeeklyAssessment Weekly);

public sealed class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<StudyAreaWeekMappingSource, StudyAreaWeekDto>()
            .ForMember(d => d.Id, o => o.MapFrom(s => s.Week.Id))
            .ForMember(d => d.WeekStartDate, o => o.MapFrom(s => s.Week.WeekStartDate))
            .ForMember(d => d.StudyAreaId, o => o.MapFrom(s => s.Week.StudyAreaId))
            .ForMember(d => d.StudyPlanId, o => o.MapFrom(s => s.Week.StudyPlanId))
            .ForMember(d => d.WeeklyAssessmentId, o => o.MapFrom(s => s.Week.WeeklyAssessmentId))
            .ForMember(d => d.WeekIndividualGoal, o => o.MapFrom(s => s.Assessment.WeekIndividualGoal))
            .ForMember(d => d.MinutesStudied, o => o.MapFrom(s => s.Assessment.MinutesStudied))
            .ForMember(
                d => d.GoalAchieved,
                o => o.MapFrom(
                    s => s.Assessment.MinutesStudied >=
                         s.Assessment.WeekIndividualGoal));
    }
}