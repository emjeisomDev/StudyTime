using Moq;
using Xunit;
using System.Globalization;
using StudyTime.Domain.Entities;
using StudyTime.Application.Dtos;
using StudyTime.Domain.Exceptions;
using StudyTime.Domain.Abstractions;
using StudyTime.Domain.ValueObjects;
using StudyTime.Domain.Abstractions.Repositories;
using StudyTime.Application.StudyRecords.Commands;
using StudyTime.Application.WeeklyAssessments.Services;

namespace StudyTime.Application.Tests.StudyRecords;

public sealed class CreateStudyRecordHandlerTests
{
    [Theory]
    [InlineData(2026, 10, 25)]
    [InlineData(2026, 10, 26)]
    public async Task Handle_DataNoDomingoOuSegundaDentroDaSemanaCorrente_CriaRegistroESincronizaDepoisDeAdicionar(
        int year,
        int month,
        int day)
    {
        // Arrange
        DateOnly localDate = new(year, month, day);
        TestContext context = new(localDate);
        CreateStudyRecordHandler handler = context.CreateHandler();
        CreateStudyRecordCommand command = new(context.TargetStudyAreaWeek.Id, 35);
        List<string> callOrder = [];

        context.StudyRecordRepository
            .Setup(item => item.AddAsync(It.IsAny<StudyRecord>(), It.IsAny<CancellationToken>()))
            .Callback<StudyRecord, CancellationToken>((_, _) => callOrder.Add("StudyRecord.Add"))
            .Returns(Task.CompletedTask);
        context.Synchronizer
            .Setup(item => item.SynchronizeAsync(context.WeeklyAssessment.Id, It.IsAny<CancellationToken>()))
            .Callback<Guid, CancellationToken>((_, _) => callOrder.Add("Synchronizer.Synchronize"))
            .Returns(Task.CompletedTask);

        // Act
        StudyRecordDto result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(localDate, result.Date);
        Assert.Equal(35, result.Minutes);
        Assert.Equal(context.TargetStudyAreaWeek.Id, result.StudyAreaWeekId);
        Assert.Equal(new[] { "StudyRecord.Add", "Synchronizer.Synchronize" }, callOrder);
        context.StudyRecordRepository.Verify(
            item => item.AddAsync(
                It.Is<StudyRecord>(record =>
                    record.Date == localDate &&
                    record.Minutes.Value == 35 &&
                    record.StudyAreaWeekId == context.TargetStudyAreaWeek.Id),
                CancellationToken.None),
            Times.Once);
        context.Synchronizer.Verify(
            item => item.SynchronizeAsync(context.WeeklyAssessment.Id, CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task Handle_StudyAreaWeekInexistente_LancaKeyNotFoundException()
    {
        // Arrange
        TestContext context = new(new DateOnly(2026, 10, 26));
        context.StudyAreaWeekRepository
            .Setup(item => item.GetByIdAsync(context.TargetStudyAreaWeek.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((StudyAreaWeek?)null);
        CreateStudyRecordHandler handler = context.CreateHandler();

        // Act
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => handler.Handle(new CreateStudyRecordCommand(context.TargetStudyAreaWeek.Id, 30), CancellationToken.None));

        // Assert
        context.WeeklyAssessmentRepository.Verify(
            item => item.GetByYearAndWeekNumberAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_StudyAreaWeekDeOutraSemana_LancaRecordNotInCurrentWeekException()
    {
        // Arrange
        TestContext context = new(new DateOnly(2026, 10, 26));
        StudyAreaWeek previousWeek = new(
            context.WeekStartDate.AddDays(-7),
            Guid.NewGuid(),
            Guid.NewGuid(),
            context.WeeklyAssessment.Id);
        context.StudyAreaWeekRepository
            .Setup(item => item.GetByIdAsync(context.TargetStudyAreaWeek.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(previousWeek);
        CreateStudyRecordHandler handler = context.CreateHandler();

        // Act
        RecordNotInCurrentWeekException exception = await Assert.ThrowsAsync<RecordNotInCurrentWeekException>(
            () => handler.Handle(new CreateStudyRecordCommand(context.TargetStudyAreaWeek.Id, 30), CancellationToken.None));

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
        TestContext context = new(new DateOnly(2026, 10, 26));
        context.WeeklyAssessmentRepository
            .Setup(item => item.GetByYearAndWeekNumberAsync(
                context.WeeklyAssessment.Year,
                context.WeeklyAssessment.WeekNumber,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((WeeklyAssessment?)null);
        CreateStudyRecordHandler handler = context.CreateHandler();

        // Act
        DomainException exception = await Assert.ThrowsAsync<DomainException>(
            () => handler.Handle(new CreateStudyRecordCommand(context.TargetStudyAreaWeek.Id, 30), CancellationToken.None));

        // Assert
        Assert.Contains("WeeklyAssessment for the configured week was not found", exception.Message);
        context.StudyRecordRepository.Verify(
            item => item.AddAsync(It.IsAny<StudyRecord>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WeeklyAssessmentNaoCorrespondeAConfiguracao_LancaDomainException()
    {
        // Arrange
        TestContext context = new(new DateOnly(2026, 10, 26));
        WeeklyAssessment differentAssessment = new(
            Guid.NewGuid(),
            context.WeeklyAssessment.WeekNumber,
            context.WeeklyAssessment.Year,
            1500m);
        context.WeeklyAssessmentRepository
            .Setup(item => item.GetByYearAndWeekNumberAsync(
                context.WeeklyAssessment.Year,
                context.WeeklyAssessment.WeekNumber,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(differentAssessment);
        CreateStudyRecordHandler handler = context.CreateHandler();

        // Act
        DomainException exception = await Assert.ThrowsAsync<DomainException>(
            () => handler.Handle(new CreateStudyRecordCommand(context.TargetStudyAreaWeek.Id, 30), CancellationToken.None));

        // Assert
        Assert.Contains("does not match its ISO week", exception.Message);
        context.StudyRecordRepository.Verify(
            item => item.AddAsync(It.IsAny<StudyRecord>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_SemanaSemConfiguracoes_LancaDomainException()
    {
        // Arrange
        TestContext context = new(new DateOnly(2026, 10, 26));
        context.ConfiguredWeeks = Array.Empty<StudyAreaWeek>();
        CreateStudyRecordHandler handler = context.CreateHandler();

        // Act
        DomainException exception = await Assert.ThrowsAsync<DomainException>(
            () => handler.Handle(new CreateStudyRecordCommand(context.TargetStudyAreaWeek.Id, 30), CancellationToken.None));

        // Assert
        Assert.Contains("No StudyAreaWeek configuration", exception.Message);
        context.StudyRecordRepository.Verify(
            item => item.AddAsync(It.IsAny<StudyRecord>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ConfiguracaoApontaParaOutraWeeklyAssessment_LancaDomainException()
    {
        // Arrange
        TestContext context = new(new DateOnly(2026, 10, 26));
        StudyAreaWeek invalidWeek = new(
            context.TargetStudyAreaWeek.WeekStartDate,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid());
        context.ConfiguredWeeks = [context.TargetStudyAreaWeek, invalidWeek];
        CreateStudyRecordHandler handler = context.CreateHandler();

        // Act
        DomainException exception = await Assert.ThrowsAsync<DomainException>(
            () => handler.Handle(new CreateStudyRecordCommand(context.TargetStudyAreaWeek.Id, 30), CancellationToken.None));

        // Assert
        Assert.Contains("same week must reference the same WeeklyAssessment", exception.Message);
    }

    [Fact]
    public async Task Handle_AvaliacaoIndividualAusenteOuVinculadaAOutraConfiguracao_LancaDomainException()
    {
        // Arrange
        TestContext context = new(new DateOnly(2026, 10, 26));
        context.IndividualAssessments[context.TargetStudyAreaWeek.Id] =
            new StudyAreaWeekAssessment(900m, context.OtherStudyAreaWeek.Id);
        CreateStudyRecordHandler handler = context.CreateHandler();

        // Act
        DomainException exception = await Assert.ThrowsAsync<DomainException>(
            () => handler.Handle(new CreateStudyRecordCommand(context.TargetStudyAreaWeek.Id, 30), CancellationToken.None));

        // Assert
        Assert.Contains("missing or mismatched", exception.Message);
        context.StudyRecordRepository.Verify(
            item => item.AddAsync(It.IsAny<StudyRecord>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_SomaDasMetasIndividuaisAbaixoDe1500_LancaWeeklyGoalNotMetException()
    {
        // Arrange
        TestContext context = new(new DateOnly(2026, 10, 26));
        context.ConfiguredWeeks = [context.TargetStudyAreaWeek];
        context.IndividualAssessments[context.TargetStudyAreaWeek.Id] =
            new StudyAreaWeekAssessment(1499m, context.TargetStudyAreaWeek.Id);
        CreateStudyRecordHandler handler = context.CreateHandler();

        // Act
        await Assert.ThrowsAsync<WeeklyGoalNotMetException>(
            () => handler.Handle(new CreateStudyRecordCommand(context.TargetStudyAreaWeek.Id, 30), CancellationToken.None));

        // Assert
        context.StudyRecordRepository.Verify(
            item => item.AddAsync(It.IsAny<StudyRecord>(), It.IsAny<CancellationToken>()), Times.Never);
        context.Synchronizer.Verify(
            item => item.SynchronizeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_MetaGlobalDiferenteDaSomaDasMetasIndividuais_LancaDomainException()
    {
        // Arrange
        TestContext context = new(new DateOnly(2026, 10, 26));
        context.IndividualAssessments[context.TargetStudyAreaWeek.Id] =
            new StudyAreaWeekAssessment(1000m, context.TargetStudyAreaWeek.Id);
        CreateStudyRecordHandler handler = context.CreateHandler();

        // Act
        DomainException exception = await Assert.ThrowsAsync<DomainException>(
            () => handler.Handle(new CreateStudyRecordCommand(context.TargetStudyAreaWeek.Id, 30), CancellationToken.None));

        // Assert
        Assert.Contains("global goal is inconsistent", exception.Message);
        context.StudyRecordRepository.Verify(
            item => item.AddAsync(It.IsAny<StudyRecord>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DataAtualForaDaSemanaConfigurada_LancaArgumentOutOfRangeException()
    {
        // Arrange
        TestContext context = new(
            new DateOnly(2026, 10, 25),
            new DateOnly(2026, 10, 26));
        CreateStudyRecordHandler handler = context.CreateHandler();

        // Act
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => handler.Handle(new CreateStudyRecordCommand(context.TargetStudyAreaWeek.Id, 30), CancellationToken.None));

        // Assert
        context.StudyRecordRepository.Verify(
            item => item.AddAsync(It.IsAny<StudyRecord>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private sealed class TestContext
    {
        private readonly FakeClock _clock;
        private readonly Mock<ICurrentWeekProvider> _currentWeekProvider = new();
        private readonly Mock<IIsoWeekCalendar> _isoWeekCalendar = new();
        public Mock<IStudyRecordRepository> StudyRecordRepository { get; } = new();
        public Mock<IStudyAreaWeekRepository> StudyAreaWeekRepository { get; } = new();
        public Mock<IWeeklyAssessmentRepository> WeeklyAssessmentRepository { get; } = new();
        public Mock<IStudyAreaWeekAssessmentRepository> StudyAreaWeekAssessmentRepository { get; } = new();
        public Mock<IWeeklyAssessmentSynchronizer> Synchronizer { get; } = new();
        public DateOnly WeekStartDate { get; }
        public WeeklyAssessment WeeklyAssessment { get; }
        public StudyAreaWeek TargetStudyAreaWeek { get; }
        public StudyAreaWeek OtherStudyAreaWeek { get; }
        public Dictionary<Guid, StudyAreaWeekAssessment?> IndividualAssessments { get; } = [];
        public IReadOnlyList<StudyAreaWeek> ConfiguredWeeks { get; set; } = Array.Empty<StudyAreaWeek>();

        public TestContext(DateOnly localDate, DateOnly? configuredWeekStart = null)
        {
            _clock = new FakeClock(new DateTime(
                localDate.Year, localDate.Month, localDate.Day, 15, 0, 0, DateTimeKind.Utc));
            WeekStartDate = configuredWeekStart ?? GetMonday(localDate);
            IsoWeek isoWeek = GetIsoWeek(WeekStartDate);
            WeeklyAssessment = new WeeklyAssessment(isoWeek.WeekNumber, isoWeek.Year, 1500m);
            TargetStudyAreaWeek = new StudyAreaWeek(
                WeekStartDate, Guid.NewGuid(), Guid.NewGuid(), WeeklyAssessment.Id);
            OtherStudyAreaWeek = new StudyAreaWeek(
                WeekStartDate, Guid.NewGuid(), Guid.NewGuid(), WeeklyAssessment.Id);
            ConfiguredWeeks = [TargetStudyAreaWeek, OtherStudyAreaWeek];
            IndividualAssessments[TargetStudyAreaWeek.Id] = new StudyAreaWeekAssessment(900m, TargetStudyAreaWeek.Id);
            IndividualAssessments[OtherStudyAreaWeek.Id] = new StudyAreaWeekAssessment(600m, OtherStudyAreaWeek.Id);

            _currentWeekProvider
                .Setup(item => item.GetCurrentWeek())
                .Returns(new WeekRange(WeekStartDate));
            _isoWeekCalendar
                .Setup(item => item.GetIsoWeek(It.IsAny<DateOnly>()))
                .Returns((DateOnly date) => GetIsoWeek(date));
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
                .Returns((int year, int weekNumber, CancellationToken _) => Task.FromResult<WeeklyAssessment?>(
                    year == WeeklyAssessment.Year && weekNumber == WeeklyAssessment.WeekNumber
                        ? WeeklyAssessment
                        : null));
            StudyAreaWeekAssessmentRepository
                .Setup(item => item.GetByStudyAreaWeekIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .Returns((Guid id, CancellationToken _) => Task.FromResult<StudyAreaWeekAssessment?>(
                    IndividualAssessments.TryGetValue(id, out StudyAreaWeekAssessment? assessment)
                        ? assessment
                        : null));
        }

        public CreateStudyRecordHandler CreateHandler() => new(
            _clock,
            _isoWeekCalendar.Object,
            _currentWeekProvider.Object,
            StudyRecordRepository.Object,
            StudyAreaWeekRepository.Object,
            WeeklyAssessmentRepository.Object,
            Synchronizer.Object,
            StudyAreaWeekAssessmentRepository.Object);

        private static DateOnly GetMonday(DateOnly date)
        {
            int daysSinceMonday = ((int)date.DayOfWeek + 6) % 7;
            return date.AddDays(-daysSinceMonday);
        }

        private static IsoWeek GetIsoWeek(DateOnly date)
        {
            DateTime value = date.ToDateTime(TimeOnly.MinValue);
            return new IsoWeek(ISOWeek.GetYear(value), ISOWeek.GetWeekOfYear(value));
        }
    }

    private sealed class FakeClock(DateTime utcNow) : IClock
    {
        public DateTime UtcNow { get; } = DateTime.SpecifyKind(utcNow, DateTimeKind.Utc);
    }
}