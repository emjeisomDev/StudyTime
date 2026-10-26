using Xunit;
using System.Globalization;
using StudyTime.Domain.ValueObjects;

namespace StudyTime.Domain.Tests.ValueObjects;

public sealed class CoefficientTests
{
    [Theory]
    [MemberData(nameof(PositiveValues))]
    public void Constructor_ValorDecimalPositivo_InstanciaCriada(decimal value)
    {
        // Arrange
        // Act
        Coefficient coefficient = new(value);

        // Assert
        Assert.Equal(value, coefficient.Value);
    }

    public static TheoryData<decimal> PositiveValues => new()
    {
        0.1m,
        1.5m,
        decimal.MaxValue
    };

    [Theory]
    [MemberData(nameof(NonPositiveValues))]
    public void Constructor_ValorZeroOuNegativo_LancaArgumentOutOfRangeException(decimal value)
    {
        // Arrange
        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => { _ = new Coefficient(value); });

        // Assert
        Assert.Equal("value", exception.ParamName);
    }

    public static TheoryData<decimal> NonPositiveValues => new()
    {
        0m,
        -0.1m,
        decimal.MinValue
    };

    [Fact]
    public void ImplicitOperatorDecimal_InstanciaValida_RetornaValor()
    {
        // Arrange
        Coefficient coefficient = new(1.75m);

        // Act
        decimal result = coefficient;

        // Assert
        Assert.Equal(1.75m, result);
    }

    [Fact]
    public void ImplicitOperatorDecimal_InstanciaNula_LancaArgumentNullException()
    {
        // Arrange
        Coefficient? coefficient = null;

        // Act
        // Assert
        Assert.Throws<ArgumentNullException>(() => _ = (decimal)coefficient!);
    }

    [Fact]
    public void ToString_CulturaPtBr_UsaSeparadorDecimalInvariante()
    {
        // Arrange
        CultureInfo originalCulture = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("pt-BR");
            Coefficient coefficient = new(1234.5m);

            // Act
            string result = coefficient.ToString();

            // Assert
            Assert.Equal("1234.5", result);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }
}