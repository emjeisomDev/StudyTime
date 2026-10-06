using AutoMapper;
using StudyTime.Application.Dtos;
using StudyTime.Domain.Entities;
using StudyTime.Domain.Services;

namespace StudyTime.Application.Mappings;

public sealed class MappingProfile : Profile
{
    private const string IndividualAssessmentsContextKey = "IndividualAssessments";

    public MappingProfile()
    {
        CreateMap<StudyArea, StudyAreaDto>()
            .ForCtorParam(
                nameof(StudyAreaDto.Id),
                options => options.MapFrom(source => source.Id))
            .ForCtorParam(
                nameof(StudyAreaDto.Name),
                options => options.MapFrom(source => source.Name))
            .ForCtorParam(
                nameof(StudyAreaDto.StdWeekStudyTime),
                options => options.MapFrom(
                    source => source.StdWeekStudyTime.Value));

        CreateMap<StudyPlan, StudyPlanDto>()
            .ForCtorParam(
                nameof(StudyPlanDto.Id),
                options => options.MapFrom(source => source.Id))
            .ForCtorParam(
                nameof(StudyPlanDto.Name),
                options => options.MapFrom(source => source.Name))
            .ForCtorParam(
                nameof(StudyPlanDto.Coefficient),
                options => options.MapFrom(
                    source => source.Coefficient.Value))
            .ForCtorParam(
                nameof(StudyPlanDto.Status),
                options => options.MapFrom(
                    source => source.Status.ToString().ToLowerInvariant()));

        CreateMap<WeeklyAssessment, WeeklyAssessmentDto>()
            .ForCtorParam(
                nameof(WeeklyAssessmentDto.Id),
                options => options.MapFrom(source => source.Id))
            .ForCtorParam(
                nameof(WeeklyAssessmentDto.WeekNumber),
                options => options.MapFrom(source => source.WeekNumber))
            .ForCtorParam(
                nameof(WeeklyAssessmentDto.Year),
                options => options.MapFrom(source => source.Year))
            .ForCtorParam(
                nameof(WeeklyAssessmentDto.WeekGlobalGoal),
                options => options.MapFrom(
                    source => source.WeekGlobalGoal))
            .ForCtorParam(
                nameof(WeeklyAssessmentDto.MinutesStudied),
                options => options.MapFrom(
                    source => source.MinutesStudied))
            .ForCtorParam(
                nameof(WeeklyAssessmentDto.GoalAchieved),
                options => options.MapFrom(
                    (source, context) =>
                        EvaluateGlobalGoalAchieved(context)));
    }

    private static bool EvaluateGlobalGoalAchieved(ResolutionContext context)
    {
        if (!context.Items.TryGetValue(IndividualAssessmentsContextKey, out var value))
        {
            throw new InvalidOperationException($"The AutoMapper context item '{IndividualAssessmentsContextKey}' is required to calculate WeeklyAssessment.GoalAchieved.");
        }

        if (value is not IEnumerable<StudyAreaWeekAssessment> assessments)
        {
            throw new InvalidOperationException($"The AutoMapper context item '{IndividualAssessmentsContextKey}' must contain StudyAreaWeekAssessment instances.");
        }

        return GoalAchievedEvaluator.EvaluateGlobal(assessments);
    }
}