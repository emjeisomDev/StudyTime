using Xunit;
using System.Reflection;
using StudyTime.Domain.Entities;
using StudyTime.Domain.Enums;
using StudyTime.Domain.Exceptions;
using StudyTime.Domain.ValueObjects;

namespace StudyTime.Domain.Tests.Entities;

public sealed class StudyPlanTests
{
    [Fact]
    public void Constructor_DadosValidos_AtribuiPropriedadesEStatusInicial()
    {
        // Arrange
        Guid id = Guid.NewGuid();
        const string name = "Plano intensivo";
        Coefficient coefficient = new(1.5m);

        // Act
        StudyPlan studyPlan = new(id, name, coefficient);

        // Assert
        Assert.Equal(id, studyPlan.Id);
        Assert.Equal(name, studyPlan.Name);
        Assert.Equal(coefficient, studyPlan.Coefficient);
        Assert.Equal(StudyPlanStatus.Active, studyPlan.Status);
    }

    [Fact]
    public void Constructor_IdVazio_LancaDomainException()
    {
        // Arrange
        // Act
        DomainException exception = Assert.Throws<DomainException>(
            () =>
            {
                _ = new StudyPlan(Guid.Empty, "Plano intensivo", new Coefficient(1.5m));
            });

        // Assert
        Assert.Contains("StudyPlan id cannot be empty.", exception.Message);
    }

    [Theory]
    [InlineData(null, "Value cannot be null.")]
    [InlineData("", "cannot be an empty string")]
    [InlineData("   ", "cannot be an empty string")]
    public void Constructor_NomeNuloVazioOuBranco_LancaArgumentException(
        string? name,
        string expectedMessage)
    {
        // Arrange
        // Act
        ArgumentException exception = Assert.ThrowsAny<ArgumentException>(
            () =>
            {
                _ = new StudyPlan(Guid.NewGuid(), name!, new Coefficient(1.5m));
            });

        // Assert
        Assert.Contains(expectedMessage.ToLowerInvariant(), exception.Message.ToLowerInvariant());
    }

    [Fact]
    public void Constructor_NomeComMaisDe80Caracteres_LancaDomainException()
    {
        // Arrange
        string name = new('A', 81);

        // Act
        DomainException exception = Assert.Throws<DomainException>(
            () =>
            {
                _ = new StudyPlan(Guid.NewGuid(), name, new Coefficient(1.5m));
            });

        // Assert
        Assert.Contains("StudyPlan name cannot exceed 80 characters.", exception.Message);
    }

    [Fact]
    public void Constructor_CoefficientNulo_LancaArgumentNullException()
    {
        // Arrange
        Coefficient? coefficient = null;

        // Act
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(
            () =>
            {
                _ = new StudyPlan(Guid.NewGuid(), "Plano intensivo", coefficient!);
            });

        // Assert
        Assert.Contains("Value cannot be null.", exception.Message);
        Assert.Contains("coefficient", exception.Message);
    }

    [Theory]
    [InlineData(StudyPlanStatus.Active)]
    [InlineData(StudyPlanStatus.Inactive)]
    public void ChangeStatus_StatusDefinido_AtualizaStatus(StudyPlanStatus status)
    {
        // Arrange
        StudyPlan studyPlan = new("Plano intensivo", new Coefficient(1.5m));

        // Act
        studyPlan.ChangeStatus(status);

        // Assert
        Assert.Equal(status, studyPlan.Status);
    }

    [Theory]
    [InlineData((StudyPlanStatus)2)]
    [InlineData((StudyPlanStatus)(-1))]
    public void ChangeStatus_StatusIndefinido_LancaDomainException(StudyPlanStatus status)
    {
        // Arrange
        StudyPlan studyPlan = new("Plano intensivo", new Coefficient(1.5m));

        // Act
        DomainException exception = Assert.Throws<DomainException>(() => studyPlan.ChangeStatus(status));

        // Assert
        Assert.Contains("Invalid StudyPlan status:", exception.Message);
    }

    [Fact]
    public void Propriedades_SemSettersPublicos_SaoImutaveisExternamente()
    {
        // Arrange
        PropertyInfo[] properties = typeof(StudyPlan).GetProperties(BindingFlags.Instance | BindingFlags.Public);

        // Act
        // Assert
        Assert.All(properties, property => Assert.Null(property.GetSetMethod()));
    }
}
