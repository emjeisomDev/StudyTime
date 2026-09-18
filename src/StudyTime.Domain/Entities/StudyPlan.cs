using StudyTime.Domain.Enums;
using StudyTime.Domain.Exceptions;

namespace StudyTime.Domain.Entities;

public sealed class StudyPlan
{
    public Guid Id { get; init; }

    public string Name { get; init; }

    public decimal Coefficient { get; init; }

    public StudyPlanStatus Status { get; private set; }

    public StudyPlan(
        Guid id,
        string name,
        decimal coefficient,
        StudyPlanStatus status)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Study plan name cannot be null, empty, or whitespace.",
                nameof(name));
        }

        if (name.Length > 80)
        {
            throw new ArgumentException(
                "Study plan name cannot exceed 80 characters.",
                nameof(name));
        }

        if (coefficient <= 0)
        {
            throw new DomainValidationException("Study plan coefficient must be greater than zero.");
        }

        if (!Enum.IsDefined(status))
        {
            throw new DomainValidationException("Study plan status must be Active or Inactive.");
        }

        Id = id;
        Name = name;
        Coefficient = coefficient;
        Status = status;
    }

    public void Activate()
    {
        Status = StudyPlanStatus.Active;
    }

    public void Deactivate()
    {
        Status = StudyPlanStatus.Inactive;
    }
}