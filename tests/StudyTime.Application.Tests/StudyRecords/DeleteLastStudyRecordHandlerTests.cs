using Moq;
using Xunit;
using MediatR;
using StudyTime.Domain.Entities;
using StudyTime.Domain.Exceptions;
using StudyTime.Domain.ValueObjects;
using StudyTime.Domain.Abstractions;
using StudyTime.Domain.Abstractions.Repositories;
using StudyTime.Application.StudyRecords.Commands;
using StudyTime.Application.WeeklyAssessments.Services;

namespace StudyTime.Application.Tests.StudyRecords;

public sealed class DeleteLastStudyRecordHandlerTests
{
    [Fact]
    public async Task Handle_RegistroElegivelEncontrado_RemoveRegistroESincronizaDepoisDaExclusao()
    {
        // Arrange
        TestContext context = new();
        DeleteLastStudyRecordHandler handler = context.CreateHandler();
        List<string> callOrder = [];
        context.StudyRecordRepository
            .Setup(item => item.Delete(context.EligibleRecord))
            .Callback(() => callOrder.Add("StudyRecord.Delete"));
        context.Synchronizer
            .Setup(item => item.SynchronizeAsync(context.WeeklyAssessment.Id, CancellationToken.None))
            .Callback(() => callOrder.Add("Synchronizer.Synchronize"))
            .Returns(Task.CompletedTask);

        // Act
        Unit result = await handler.Handle(
            new DeleteLastStudyRecordCommand(context.TargetStudyAreaWeek.Id),
            CancellationToken.None);

        // Assert
        Assert.Equal(Unit.Value, result);
        Assert.Equal(new[] { "StudyRecord.Delete", "Synchronizer.Synchronize" }, callOrder);
        context.StudyRecordRepository.Verify(item => item.Delete(context.EligibleRecord), Times.Once);
        context.Synchronizer.Verify(
            item => item.SynchronizeAsync(context.WeeklyAssessment.Id, CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task Handle_IdDaConfiguracaoVazio_LancaArgumentException()
    {
        // Arrange
        TestContext context = new();
        DeleteLastStudyRecordHandler handler = context.CreateHandler();

        // Act
        await Assert.ThrowsAsync<ArgumentException>(
            () => handler.Handle(new DeleteLastStudyRecordCommand(Guid.Empty), CancellationToken.None));

        // Assert
        context.StudyAreaWeekRepository.Verify(
            item => item.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_StudyAreaWeekInexistente_LancaKeyNotFoundException()
    {
        // Arrange
        TestContext context = new();
        context.StudyAreaWeekRepository
            .Setup(item => item.GetByIdAsync(context.TargetStudyAreaWeek.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((StudyAreaWeek?)null);
        DeleteLastStudyRecordHandler handler = context.CreateHandler();

        // Act
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => handler.Handle(new DeleteLastStudyRecordCommand(context.TargetStudyAreaWeek.Id), CancellationToken.None));

        // Assert
        context.StudyRecordLifoSelector.Verify(
            item => item.SelectLastEligibleAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_StudyAreaWeekDeOutraSemana_LancaRecordNotInCurrentWeekException()
    {
        // Arrange
        TestContext context = new();
        StudyAreaWeek previousWeek = new(
            context.WeekStartDate.AddDays(-7), Guid.NewGuid(), Guid.NewGuid(), context.WeeklyAssessment.Id);
        context.StudyAreaWeekRepository
            .Setup(item => item.GetByIdAsync(context.TargetStudyAreaWeek.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(previousWeek);
        DeleteLastStudyRecordHandler handler = context.CreateHandler();

        // Act
        RecordNotInCurrentWeekException exception = await Assert.ThrowsAsync<RecordNotInCurrentWeekException>(
            () => handler.Handle(new DeleteLastStudyRecordCommand(context.TargetStudyAreaWeek.Id), CancellationToken.None));

        // Assert
        Assert.Contains("only allowed for the current week", exception.Message);
        context.WeeklyAssessmentRepository.Verify(
            item => item.GetByYearAndWeekNumberAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WeeklyAssessmentInexistente_LancaDomainException()
    {
        // Arrange
        TestContext context = new();
        context.WeeklyAssessmentRepository
            .Setup(item => item.GetByYearAndWeekNumberAsync(
                context.WeeklyAssessment.Year,
                context.WeeklyAssessment.WeekNumber,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((WeeklyAssessment?)null);
        DeleteLastStudyRecordHandler handler = context.CreateHandler();

        // Act
        DomainException exception = await Assert.ThrowsAsync<DomainException>(
            () => handler.Handle(new DeleteLastStudyRecordCommand(context.TargetStudyAreaWeek.Id), CancellationToken.None));

        // Assert
        Assert.Contains("WeeklyAssessment for the configured week was not found", exception.Message);
    }

    [Fact]
    public async Task Handle_WeeklyAssessmentNaoCorrespondeAConfiguracao_LancaDomainException()
    {
        // Arrange
        TestContext context = new();
        WeeklyAssessment different = new(
            Guid.NewGuid(), context.WeeklyAssessment.WeekNumber, context.WeeklyAssessment.Year, 1500m);
        context.WeeklyAssessmentRepository
            .Setup(item => item.GetByYearAndWeekNumberAsync(
                context.WeeklyAssessment.Year,
                context.WeeklyAssessment.WeekNumber,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(different);
        DeleteLastStudyRecordHandler handler = context.CreateHandler();

        // Act
        DomainException exception = await Assert.ThrowsAsync<DomainException>(
            () => handler.Handle(new DeleteLastStudyRecordCommand(context.TargetStudyAreaWeek.Id), CancellationToken.None));

        // Assert
        Assert.Contains("does not match its ISO week", exception.Message);
    }

    [Fact]
    public async Task Handle_AvaliacaoIndividualAusenteOuVinculadaAOutraConfiguracao_LancaDomainException()
    {
        // Arrange
        TestContext context = new();
        context.IndividualAssessments[context.TargetStudyAreaWeek.Id] =
            new StudyAreaWeekAssessment(900m, context.OtherStudyAreaWeek.Id);
        DeleteLastStudyRecordHandler handler = context.CreateHandler();

        // Act
        DomainException exception = await Assert.ThrowsAsync<DomainException>(
            () => handler.Handle(new DeleteLastStudyRecordCommand(context.TargetStudyAreaWeek.Id), CancellationToken.None));

        // Assert
        Assert.Contains("missing or mismatched", exception.Message);
        context.StudyRecordLifoSelector.Verify(
            item => item.SelectLastEligibleAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_SemConfiguracoesParaSemana_LancaDomainException()
    {
        // Arrange
        TestContext context = new();
        context.ConfiguredWeeks = Array.Empty<StudyAreaWeek>();
        DeleteLastStudyRecordHandler handler = context.CreateHandler();

        // Act
        DomainException exception = await Assert.ThrowsAsync<DomainException>(
            () => handler.Handle(new DeleteLastStudyRecordCommand(context.TargetStudyAreaWeek.Id), CancellationToken.None));

        // Assert
        Assert.Contains("No StudyAreaWeek configuration", exception.Message);
        context.StudyRecordLifoSelector.Verify(
            item => item.SelectLastEligibleAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ConfiguracaoDaMesmaSemanaApontaParaOutraWeeklyAssessment_LancaDomainException()
    {
        // Arrange
        TestContext context = new();
        StudyAreaWeek otherWithWrongAssessment = new(
            context.WeekStartDate, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        context.ConfiguredWeeks = [context.TargetStudyAreaWeek, otherWithWrongAssessment];
        DeleteLastStudyRecordHandler handler = context.CreateHandler();

        // Act
        DomainException exception = await Assert.ThrowsAsync<DomainException>(
            () => handler.Handle(new DeleteLastStudyRecordCommand(context.TargetStudyAreaWeek.Id), CancellationToken.None));

        // Assert
        Assert.Contains("same WeeklyAssessment", exception.Message);
        context.StudyRecordLifoSelector.Verify(
            item => item.SelectLastEligibleAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_SeletorLifoNaoEncontraRegistroElegivel_LancaNoEligibleStudyRecordException()
    {
        // Arrange
        TestContext context = new();
        context.StudyRecordLifoSelector
            .Setup(item => item.SelectLastEligibleAsync(context.TargetStudyAreaWeek.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((StudyRecord?)null);
        DeleteLastStudyRecordHandler handler = context.CreateHandler();

        // Act
        await Assert.ThrowsAsync<NoEligibleStudyRecordException>(
            () => handler.Handle(new DeleteLastStudyRecordCommand(context.TargetStudyAreaWeek.Id), CancellationToken.None));

        // Assert
        context.StudyRecordRepository.Verify(item => item.Delete(It.IsAny<StudyRecord>()), Times.Never);
        context.Synchronizer.Verify(
            item => item.SynchronizeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_SeletorRetornaRegistroDeOutraConfiguracao_LancaDomainException()
    {
        // Arrange
        TestContext context = new();
        StudyRecord invalidRecord = new(
            context.WeekStartDate,
            new Minutes(20),
            context.OtherStudyAreaWeek.Id);
        context.StudyRecordLifoSelector
            .Setup(item => item.SelectLastEligibleAsync(context.TargetStudyAreaWeek.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invalidRecord);
        DeleteLastStudyRecordHandler handler = context.CreateHandler();

        // Act
        DomainException exception = await Assert.ThrowsAsync<DomainException>(
            () => handler.Handle(new DeleteLastStudyRecordCommand(context.TargetStudyAreaWeek.Id), CancellationToken.None));

        // Assert
        Assert.Contains("does not belong to the requested StudyAreaWeek", exception.Message);
        context.StudyRecordRepository.Verify(item => item.Delete(It.IsAny<StudyRecord>()), Times.Never);
    }

    private sealed class TestContext
    {
        private readonly Mock<IIsoWeekCalendar> _isoWeekCalendar = new();
        private readonly Mock<ICurrentWeekProvider> _currentWeekProvider = new();
        public Mock<IStudyRecordRepository> StudyRecordRepository { get; } = new();
        public Mock<IStudyAreaWeekRepository> StudyAreaWeekRepository { get; } = new();
        public Mock<IStudyRecordLifoSelector> StudyRecordLifoSelector { get; } = new();
        public Mock<IWeeklyAssessmentRepository> WeeklyAssessmentRepository { get; } = new();
        public Mock<IStudyAreaWeekAssessmentRepository> StudyAreaWeekAssessmentRepository { get; } = new();
        public Mock<IWeeklyAssessmentSynchronizer> Synchronizer { get; } = new();
        public DateOnly WeekStartDate { get; } = new(2026, 10, 26);
        public WeeklyAssessment WeeklyAssessment { get; }
        public StudyAreaWeek TargetStudyAreaWeek { get; }
        public StudyAreaWeek OtherStudyAreaWeek { get; }
        public StudyRecord EligibleRecord { get; }
        public Dictionary<Guid, StudyAreaWeekAssessment?> IndividualAssessments { get; } = [];
        public IReadOnlyList<StudyAreaWeek> ConfiguredWeeks { get; set; } = Array.Empty<StudyAreaWeek>();

        public TestContext()
        {
            DateTime weekStartDateTime = WeekStartDate.ToDateTime(TimeOnly.MinValue);
            int year = System.Globalization.ISOWeek.GetYear(weekStartDateTime);
            int weekNumber = System.Globalization.ISOWeek.GetWeekOfYear(weekStartDateTime);
            WeeklyAssessment = new WeeklyAssessment(weekNumber, year, 1500m);
            TargetStudyAreaWeek = new StudyAreaWeek(
                WeekStartDate, Guid.NewGuid(), Guid.NewGuid(), WeeklyAssessment.Id);
            OtherStudyAreaWeek = new StudyAreaWeek(
                WeekStartDate, Guid.NewGuid(), Guid.NewGuid(), WeeklyAssessment.Id);
            EligibleRecord = new StudyRecord(WeekStartDate, new Minutes(25), TargetStudyAreaWeek.Id);
            ConfiguredWeeks = [TargetStudyAreaWeek, OtherStudyAreaWeek];
            IndividualAssessments[TargetStudyAreaWeek.Id] = new StudyAreaWeekAssessment(900m, TargetStudyAreaWeek.Id);
            IndividualAssessments[OtherStudyAreaWeek.Id] = new StudyAreaWeekAssessment(600m, OtherStudyAreaWeek.Id);

            _currentWeekProvider
                .Setup(item => item.GetCurrentWeek())
                .Returns(new WeekRange(WeekStartDate));
            _isoWeekCalendar
                .Setup(item => item.GetIsoWeek(It.IsAny<DateOnly>()))
                .Returns((DateOnly date) =>
                {
                    DateTime value = date.ToDateTime(TimeOnly.MinValue);
                    return new IsoWeek(
                        System.Globalization.ISOWeek.GetYear(value),
                        System.Globalization.ISOWeek.GetWeekOfYear(value));
                });
            StudyAreaWeekRepository
                .Setup(item => item.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .Returns((Guid id, CancellationToken _) => Task.FromResult<StudyAreaWeek?>(
                    id == TargetStudyAreaWeek.Id ? TargetStudyAreaWeek :
                    id == OtherStudyAreaWeek.Id ? OtherStudyAreaWeek : null));
            StudyAreaWeekRepository
                .Setup(item => item.GetByWeekStartDateAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
                .Returns((DateOnly date, CancellationToken _) => Task.FromResult<IReadOnlyList<StudyAreaWeek>>(
                    date == WeekStartDate ? ConfiguredWeeks : Array.Empty<StudyAreaWeek>()));
            WeeklyAssessmentRepository
                .Setup(item => item.GetByYearAndWeekNumberAsync(
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(WeeklyAssessment);
            StudyAreaWeekAssessmentRepository
                .Setup(item => item.GetByStudyAreaWeekIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .Returns((Guid id, CancellationToken _) => Task.FromResult<StudyAreaWeekAssessment?>(
                    IndividualAssessments.TryGetValue(id, out StudyAreaWeekAssessment? assessment)
                        ? assessment
                        : null));
            StudyRecordLifoSelector
                .Setup(item => item.SelectLastEligibleAsync(TargetStudyAreaWeek.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(EligibleRecord);
        }

        public DeleteLastStudyRecordHandler CreateHandler() => new(
            _isoWeekCalendar.Object,
            _currentWeekProvider.Object,
            StudyRecordRepository.Object,
            StudyAreaWeekRepository.Object,
            StudyRecordLifoSelector.Object,
            WeeklyAssessmentRepository.Object,
            Synchronizer.Object,
            StudyAreaWeekAssessmentRepository.Object);
    }
}