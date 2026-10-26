using Xunit;
using System.Globalization;
using StudyTime.Domain.ValueObjects;

namespace StudyTime.Domain.Tests.ValueObjects;

public sealed class MinutesTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(90)]
    [InlineData(int.MaxValue)]
    public void Constructor_ValorPositivo_InstanciaCriada(int value)
    {
        // Arrange
        // Act
        Minutes minutes = new(value);

        // Assert
        Assert.Equal(value, minutes.Value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void Constructor_ValorZeroOuNegativo_LancaArgumentOutOfRangeException(int value)
    {
        // Arrange
        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => { _ = new Minutes(value); });

        // Assert
        Assert.Equal("value", exception.ParamName);
    }

    [Fact]
    public void ImplicitOperatorInt_InstanciaValida_RetornaValor()
    {
        // Arrange
        Minutes minutes = new(42);

        // Act
        int result = minutes;

        // Assert
        Assert.Equal(42, result);
    }

    [Fact]
    public void ImplicitOperatorInt_InstanciaNula_LancaArgumentNullException()
    {
        // Arrange
        Minutes? minutes = null;

        // Act
        // Assert
        Assert.Throws<ArgumentNullException>(() => _ = (int)minutes!);
    }

    [Fact]
    public void ToString_CulturaPtBr_UsaFormatoInvariante()
    {
        // Arrange
        CultureInfo originalCulture = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("pt-BR");
            Minutes minutes = new(1234);

            // Act
            string result = minutes.ToString();

            // Assert
            Assert.Equal("1234", result);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }
}