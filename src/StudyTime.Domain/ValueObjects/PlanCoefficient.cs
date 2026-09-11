using StudyTime.Domain.Exceptions;

namespace StudyTime.Domain.ValueObjects;

public readonly record struct PlanCoefficient
{
    public decimal Value { get; }

    private PlanCoefficient(decimal value)
    {
        Value = value;
    }

    public static PlanCoefficient From(decimal value)
    {
        if (value <= 0)
        {
            throw DomainRuleViolationException.R13_CoefficientMustBePositive();
        }

        return new PlanCoefficient(value);
    }

    public static implicit operator decimal(PlanCoefficient coefficient)
        => coefficient.Value;
    
    public override string ToString()
        => Value.ToString();
    
}