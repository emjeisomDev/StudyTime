
using StudyTime.Domain.Enums;
using StudyTime.Domain.Exceptions;
using StudyTime.Domain.ValueObjects;

namespace StudyTime.Domain.Entities;

public sealed class StudyPlan
{
    public Guid Id { get; }
    public string Name { get; }
    public Coefficient Coefficient { get; }
    public StudyPlanStatus Status { get; private set; }

    public StudyPlan(string name, Coefficient coefficient)
        : this(Guid.NewGuid(), name, coefficient)
    {
    }

    public StudyPlan(Guid id, string name, Coefficient coefficient)
    {
        if (id == Guid.Empty)
        {
            throw new DomainException("StudyPlan id cannot be empty.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (name.Length > 80)
        {
            throw new DomainException("StudyPlan name cannot exceed 80 characters.");
        }

        ArgumentNullException.ThrowIfNull(coefficient);

        Id = id;
        Name = name;
        Coefficient = coefficient;
        Status = StudyPlanStatus.Active;
    }

    public void ChangeStatus(StudyPlanStatus status)
    {
        if (!Enum.IsDefined(status))
        {
            throw new DomainException($"Invalid StudyPlan status: {status}.");
        }

        Status = status;
    }
}