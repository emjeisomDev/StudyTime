using StudyTime.Application.Validators;
using StudyTime.Application.StudyRecords.Commands;

namespace StudyTime.Application.Tests.Validators;

public sealed class CreateStudyRecordCommandValidatorTests
{
    private readonly CreateStudyRecordCommandValidator _validator = new();

    [Fact]
    public async Task Validate_validCommand_returnsValid()
    {
        var command =
            new CreateStudyRecordCommand(
                Guid.NewGuid(),
                60);

        var result =
            await _validator.ValidateAsync(command);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task Validate_emptyStudyAreaWeekId_returnsValidationError()
    {
        var command =
            new CreateStudyRecordCommand(
                Guid.Empty,
                60);

        var result =
            await _validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            error =>
                error.PropertyName ==
                nameof(CreateStudyRecordCommand.StudyAreaWeekId));
    }

    [Fact]
    public async Task Validate_zeroMinutes_returnsValidationError()
    {
        var command =
            new CreateStudyRecordCommand(
                Guid.NewGuid(),
                0);

        var result =
            await _validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            error =>
                error.PropertyName ==
                nameof(CreateStudyRecordCommand.Minutes));
    }

    [Fact]
    public async Task Validate_negativeMinutes_returnsValidationError()
    {
        var command =
            new CreateStudyRecordCommand(
                Guid.NewGuid(),
                -10);

        var result =
            await _validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            error =>
                error.PropertyName ==
                nameof(CreateStudyRecordCommand.Minutes));
    }

    [Fact]
    public async Task Validate_emptyStudyAreaWeekIdAndInvalidMinutes_returnsBothValidationErrors()
    {
        var command =
            new CreateStudyRecordCommand(
                Guid.Empty,
                0);

        var result =
            await _validator.ValidateAsync(command);

        Assert.False(result.IsValid);

        Assert.Contains(
            result.Errors,
            error =>
                error.PropertyName ==
                nameof(CreateStudyRecordCommand.StudyAreaWeekId));

        Assert.Contains(
            result.Errors,
            error =>
                error.PropertyName ==
                nameof(CreateStudyRecordCommand.Minutes));
    }
}