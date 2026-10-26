namespace StudyTime.Api.Contracts.Responses;

/// <summary>Resposta da avaliação global de uma semana.</summary>
/// <param name="Id">Identificador da avaliação semanal.</param>
/// <param name="WeekNumber">Número da semana ISO.</param>
/// <param name="Year">Ano ISO da semana.</param>
/// <param name="WeekGlobalGoal">Meta global semanal calculada a partir das metas individuais.</param>
/// <param name="MinutesStudied">Total de minutos estudados na semana.</param>
/// <param name="GoalAchieved">Indica o atingimento simultâneo de todas as metas individuais. É true se, e somente se, existir ao menos uma avaliação individual e todas as metas individuais tiverem sido atingidas; é false se não houver avaliações individuais ou se pelo menos uma meta individual não tiver sido atingida. Não é uma comparação direta entre MinutesStudied e WeekGlobalGoal. Valor derivado por GoalAchievedEvaluator.EvaluateGlobal.</param>
public sealed record WeeklyAssessmentResponse(
    Guid Id,
    int WeekNumber,
    int Year,
    decimal WeekGlobalGoal,
    int MinutesStudied,
    bool GoalAchieved);
