using StudyTime.Domain.Enums;
using StudyTime.Domain.ValueObjects;

namespace StudyTime.Domain.Entities;

public sealed class StudyPlan
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public decimal Coefficient { get; private set; }
    public StudyPlanStatus Status { get; private set; }

    private StudyPlan() { }

    private StudyPlan(Guid id, string name, PlanCoefficient coefficient, StudyPlanStatus status)
    {
        Id = id;
        Name = name;
        Coefficient = coefficient.Value;
        Status = status;
    }

    public static StudyPlan Create(string name, decimal coefficient, StudyPlanStatus status = StudyPlanStatus.Active, Guid? id = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var planCoefficient = PlanCoefficient.From(coefficient);

        return new StudyPlan(
            id ?? Guid.NewGuid(),
            name.Trim(),
            planCoefficient,
            status);
    }
}