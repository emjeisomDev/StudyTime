using Xunit;
using MediatR;
using FluentValidation;
using StudyTime.Application.Behaviors;

namespace StudyTime.Application.Tests.Behaviors;

public sealed class ValidationBehaviorTests
{
    [Fact]
    public async Task Handle_SemValidators_ChamaNext()
    {
        // Arrange
        FakeRequest request = new(1, "válido");
        ValidationBehavior<FakeRequest, string> behavior = new(Array.Empty<IValidator<FakeRequest>>());
        bool nextCalled = false;
        CancellationToken receivedToken = default;
        using CancellationTokenSource cancellationTokenSource = new();
        CancellationToken cancellationToken = cancellationTokenSource.Token;
        RequestHandlerDelegate<string> next = token =>
        {
            nextCalled = true;
            receivedToken = token;
            return Task.FromResult("next executado");
        };

        // Act
        string result = await behavior.Handle(request, next, cancellationToken);

        // Assert
        Assert.Equal("next executado", result);
        Assert.True(nextCalled);
        Assert.Equal(cancellationToken, receivedToken);
    }

    [Fact]
    public async Task Handle_UmValidatorValido_ChamaNext()
    {
        // Arrange
        FakeRequest request = new(5, "válido");
        ValidationBehavior<FakeRequest, string> behavior = new([new PositiveValueValidator()]);
        bool nextCalled = false;
        RequestHandlerDelegate<string> next = _ =>
        {
            nextCalled = true;
            return Task.FromResult("sucesso");
        };

        // Act
        string result = await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        Assert.Equal("sucesso", result);
        Assert.True(nextCalled);
    }

    [Fact]
    public async Task Handle_UmValidatorComErro_LancaValidationExceptionESemChamarNext()
    {
        // Arrange
        FakeRequest request = new(0, "válido");
        ValidationBehavior<FakeRequest, string> behavior = new(
            [new PositiveValueValidator()]);
        bool nextCalled = false;
        RequestHandlerDelegate<string> next = _ =>
        {
            nextCalled = true;
            return Task.FromResult("não deveria executar");
        };

        // Act
        ValidationException exception = await Assert.ThrowsAsync<ValidationException>(
            () => behavior.Handle(request, next, CancellationToken.None));

        // Assert
        Assert.Single(exception.Errors);
        Assert.Contains(
            exception.Errors,
            error => error.ErrorMessage == "Value must be positive.");
        Assert.False(nextCalled);
    }

    [Fact]
    public async Task Handle_MultiplosValidatorsComErros_AgregaErrosESemChamarNext()
    {
        // Arrange
        FakeRequest request = new(0, string.Empty);
        ValidationBehavior<FakeRequest, string> behavior = new(
        [
            new PositiveValueValidator(),
            new RequiredNameValidator()
        ]);
        bool nextCalled = false;
        RequestHandlerDelegate<string> next = _ =>
        {
            nextCalled = true;
            return Task.FromResult("não deveria executar");
        };

        // Act
        ValidationException exception = await Assert.ThrowsAsync<ValidationException>(
            () => behavior.Handle(request, next, CancellationToken.None));

        // Assert
        Assert.Equal(2, exception.Errors.Count());
        Assert.Contains(
            exception.Errors,
            error => error.ErrorMessage == "Value must be positive.");
        Assert.Contains(
            exception.Errors,
            error => error.ErrorMessage == "Name is required.");
        Assert.False(nextCalled);
    }

    private sealed record FakeRequest(int Value, string Name);

    private sealed class PositiveValueValidator : AbstractValidator<FakeRequest>
    {
        public PositiveValueValidator()
        {
            RuleFor(request => request.Value)
                .GreaterThan(0)
                .WithMessage("Value must be positive.");
        }
    }

    private sealed class RequiredNameValidator : AbstractValidator<FakeRequest>
    {
        public RequiredNameValidator()
        {
            RuleFor(request => request.Name)
                .NotEmpty()
                .WithMessage("Name is required.");
        }
    }
}