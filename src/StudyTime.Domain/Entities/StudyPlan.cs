using StudyTime.Domain.Enums;

namespace StudyTime.Domain.Entities;

public sealed class StudyPlan
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public decimal Coefficient { get; private set; }
    public StudyPlanStatus Status { get; private set; }

    private StudyPlan()
    {
        Name = string.Empty;
    }

    private StudyPlan(Guid id, string name, decimal coefficient, StudyPlanStatus status)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("The study plan id must not be empty.", nameof(id));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("The study plan name is required.", nameof(name));
        if (name.Length > 80)
            throw new ArgumentException("The study plan name must contain at most 80 characters.", nameof(name));
        if (coefficient <= 0)
            throw new ArgumentOutOfRangeException(nameof(coefficient), "The coefficient must be greater than zero.");
        if (!Enum.IsDefined(status))
            throw new ArgumentOutOfRangeException(nameof(status), "The study plan status is invalid.");

        Id = id;
        Name = name.Trim();
        Coefficient = coefficient;
        Status = status;
    }

    public static StudyPlan Create(string name, decimal coefficient, StudyPlanStatus status = StudyPlanStatus.Active)
        => new(Guid.NewGuid(), name, coefficient, status);

    public static StudyPlan Create(Guid id, string name, decimal coefficient, StudyPlanStatus status = StudyPlanStatus.Active)
        => new(id, name, coefficient, status);

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("The study plan name is required.", nameof(name));
        if (name.Length > 80)
            throw new ArgumentException("The study plan name must contain at most 80 characters.", nameof(name));

        Name = name.Trim();
    }

    public void ChangeCoefficient(decimal coefficient)
    {
        if (coefficient <= 0)
            throw new ArgumentOutOfRangeException(nameof(coefficient), "The coefficient must be greater than zero.");

        Coefficient = coefficient;
    }

    public void Activate() => Status = StudyPlanStatus.Active;

    public void Deactivate() => Status = StudyPlanStatus.Inactive;
}