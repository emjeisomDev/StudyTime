using Moq;
using Xunit;
using System.Globalization;
using StudyTime.Domain.Entities;
using StudyTime.Domain.Exceptions;
using StudyTime.Domain.Abstractions;
using StudyTime.Domain.ValueObjects;
using StudyTime.Domain.Abstractions.Repositories;
using StudyTime.Application.WeeklyAssessments.Services;

namespace StudyTime.Application.Tests.StudyRecords;

public sealed class WeeklyAssessmentSynchronizerTests
{
    [Fact]
    public async Task SynchronizeAsync_MultiplasConfiguracoes_RecalculaMinutosMetasEExecutaDoisSaveChanges()
    {
        // Arrange
        TestContext context = new();
        List<string> callOrder = [];
        context.UnitOfWork
            .Setup(item => item.SaveChangesAsync(CancellationToken.None))
            .Callback(() => callOrder.Add("UnitOfWork.SaveChanges"))
            .ReturnsAsync(1);

        context.WeeklyAssessmentRepository
            .Setup(item => item.GetByIdAsync(context.WeeklyAssessment.Id, CancellationToken.None))
            .Callback(() => callOrder.Add("WeeklyAssessment.Get"))
            .ReturnsAsync(context.WeeklyAssessment);
            
        WeeklyAssessmentSynchronizer synchronizer = context.CreateSynchronizer();

        // Act
        await synchronizer.SynchronizeAsync(context.WeeklyAssessment.Id, CancellationToken.None);

        // Assert
        Assert.Equal(200, context.TargetAssessment.MinutesStudied);
        Assert.Equal(400, context.OtherAssessment.MinutesStudied);
        Assert.Equal(1500m, context.WeeklyAssessment.WeekGlobalGoal);
        Assert.Equal(600, context.WeeklyAssessment.MinutesStudied);
        context.StudyAreaWeekAssessmentRepository.Verify(item => item.Update(context.TargetAssessment), Times.Once);
        context.StudyAreaWeekAssessmentRepository.Verify(item => item.Update(context.OtherAssessment), Times.Once);
        context.WeeklyAssessmentRepository.Verify(item => item.Update(context.WeeklyAssessment), Times.Once);
        context.UnitOfWork.Verify(item => item.SaveChangesAsync(CancellationToken.None), Times.Exactly(2));
        Assert.Equal(
            new[] { "UnitOfWork.SaveChanges", "WeeklyAssessment.Get", "UnitOfWork.SaveChanges" },
            callOrder);
    }

    [Fact]
    public async Task SynchronizeAsync_IdDaWeeklyAssessmentVazio_LancaArgumentExceptionAntesDeSalvar()
    {
        // Arrange
        TestContext context = new();
        WeeklyAssessmentSynchronizer synchronizer = context.CreateSynchronizer();

        // Act
        await Assert.ThrowsAsync<ArgumentException>(
            () => synchronizer.SynchronizeAsync(Guid.Empty, CancellationToken.None));

        // Assert
        context.UnitOfWork.Verify(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SynchronizeAsync_WeeklyAssessmentInexistente_LancaKeyNotFoundException()
    {
        // Arrange
        TestContext context = new();
        context.WeeklyAssessmentRepository
            .Setup(item => item.GetByIdAsync(context.WeeklyAssessment.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((WeeklyAssessment?)null);
        WeeklyAssessmentSynchronizer synchronizer = context.CreateSynchronizer();

        // Act
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => synchronizer.SynchronizeAsync(context.WeeklyAssessment.Id, CancellationToken.None));

        // Assert
        context.UnitOfWork.Verify(item => item.SaveChangesAsync(CancellationToken.None), Times.Once);
        context.StudyAreaWeekRepository.Verify(
            item => item.GetByWeekStartDateAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SynchronizeAsync_SemConfiguracoesNaSemanaIso_LancaDomainException()
    {
        // Arrange
        TestContext context = new();
        context.ConfiguredWeeks = Array.Empty<StudyAreaWeek>();
        WeeklyAssessmentSynchronizer synchronizer = context.CreateSynchronizer();

        // Act
        DomainException exception = await Assert.ThrowsAsync<DomainException>(
            () => synchronizer.SynchronizeAsync(context.WeeklyAssessment.Id, CancellationToken.None));

        // Assert
        Assert.Contains("No StudyAreaWeek configuration was found", exception.Message);
        context.UnitOfWork.Verify(item => item.SaveChangesAsync(CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task SynchronizeAsync_SemanaIsoDaAvaliacaoInconsistente_LancaDomainException()
    {
        // Arrange
        TestContext context = new();
        context.IsoWeekCalendar
            .Setup(item => item.GetMonday(context.WeeklyAssessment.Year, context.WeeklyAssessment.WeekNumber))
            .Returns(context.WeekStartDate.AddDays(7));
        WeeklyAssessmentSynchronizer synchronizer = context.CreateSynchronizer();

        // Act
        DomainException exception = await Assert.ThrowsAsync<DomainException>(
            () => synchronizer.SynchronizeAsync(context.WeeklyAssessment.Id, CancellationToken.None));

        // Assert
        Assert.Contains("ISO week is inconsistent", exception.Message);
        context.StudyAreaWeekRepository.Verify(
            item => item.GetByWeekStartDateAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SynchronizeAsync_StudyAreaWeekVinculadaAOutraWeeklyAssessment_LancaDomainException()
    {
        // Arrange
        TestContext context = new();
        StudyAreaWeek invalidWeek = new(
            context.WeekStartDate, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        context.ConfiguredWeeks = [context.TargetStudyAreaWeek, invalidWeek];
        WeeklyAssessmentSynchronizer synchronizer = context.CreateSynchronizer();

        // Act
        DomainException exception = await Assert.ThrowsAsync<DomainException>(
            () => synchronizer.SynchronizeAsync(context.WeeklyAssessment.Id, CancellationToken.None));

        // Assert
        Assert.Contains("associated with a WeeklyAssessment belonging to another week", exception.Message);
        context.UnitOfWork.Verify(item => item.SaveChangesAsync(CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task SynchronizeAsync_AvaliacaoIndividualInexistente_LancaDomainException()
    {
        // Arrange
        TestContext context = new();
        context.IndividualAssessments.Remove(context.TargetStudyAreaWeek.Id);
        WeeklyAssessmentSynchronizer synchronizer = context.CreateSynchronizer();

        // Act
        DomainException exception = await Assert.ThrowsAsync<DomainException>(
            () => synchronizer.SynchronizeAsync(context.WeeklyAssessment.Id, CancellationToken.None));

        // Assert
        Assert.Contains("StudyAreaWeekAssessment was not found", exception.Message);
        context.UnitOfWork.Verify(item => item.SaveChangesAsync(CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task SynchronizeAsync_AvaliacaoIndividualVinculadaAOutraConfiguracao_LancaDomainException()
    {
        // Arrange
        TestContext context = new();
        context.IndividualAssessments[context.TargetStudyAreaWeek.Id] =
            new StudyAreaWeekAssessment(900m, context.OtherStudyAreaWeek.Id);
        WeeklyAssessmentSynchronizer synchronizer = context.CreateSynchronizer();

        // Act
        DomainException exception = await Assert.ThrowsAsync<DomainException>(
            () => synchronizer.SynchronizeAsync(context.WeeklyAssessment.Id, CancellationToken.None));

        // Assert
        Assert.Contains("does not belong to its StudyAreaWeek", exception.Message);
    }

    [Fact]
    public async Task SynchronizeAsync_StudyRecordVinculadoAOutraConfiguracao_LancaDomainException()
    {
        // Arrange
        TestContext context = new();
        StudyRecord invalidRecord = new(
            context.WeekStartDate,
            new Minutes(15),
            context.OtherStudyAreaWeek.Id);
        context.RecordsByWeek[context.TargetStudyAreaWeek.Id] = [invalidRecord];
        WeeklyAssessmentSynchronizer synchronizer = context.CreateSynchronizer();

        // Act
        DomainException exception = await Assert.ThrowsAsync<DomainException>(
            () => synchronizer.SynchronizeAsync(context.WeeklyAssessment.Id, CancellationToken.None));

        // Assert
        Assert.Contains("unexpected StudyAreaWeek", exception.Message);
        context.UnitOfWork.Verify(item => item.SaveChangesAsync(CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task SynchronizeAsync_SomaDasMetasAbaixoDe1500_LancaWeeklyGoalNotMetException()
    {
        // Arrange
        TestContext context = new();
        context.IndividualAssessments[context.TargetStudyAreaWeek.Id] =
            new StudyAreaWeekAssessment(700m, context.TargetStudyAreaWeek.Id);
        context.IndividualAssessments[context.OtherStudyAreaWeek.Id] =
            new StudyAreaWeekAssessment(700m, context.OtherStudyAreaWeek.Id);
        WeeklyAssessmentSynchronizer synchronizer = context.CreateSynchronizer();

        // Act
        await Assert.ThrowsAsync<WeeklyGoalNotMetException>(
            () => synchronizer.SynchronizeAsync(context.WeeklyAssessment.Id, CancellationToken.None));

        // Assert
        context.UnitOfWork.Verify(item => item.SaveChangesAsync(CancellationToken.None), Times.Once);
        context.WeeklyAssessmentRepository.Verify(item => item.Update(context.WeeklyAssessment), Times.Never);
    }

    private sealed class TestContext
    {
        public Mock<IUnitOfWork> UnitOfWork { get; } = new();
        public Mock<IIsoWeekCalendar> IsoWeekCalendar { get; } = new();
        public Mock<IStudyRecordRepository> StudyRecordRepository { get; } = new();
        public Mock<IStudyAreaWeekRepository> StudyAreaWeekRepository { get; } = new();
        public Mock<IWeeklyAssessmentRepository> WeeklyAssessmentRepository { get; } = new();
        public Mock<IStudyAreaWeekAssessmentRepository> StudyAreaWeekAssessmentRepository { get; } = new();
        public DateOnly WeekStartDate { get; } = new(2026, 10, 26);
        public WeeklyAssessment WeeklyAssessment { get; }
        public StudyAreaWeek TargetStudyAreaWeek { get; }
        public StudyAreaWeek OtherStudyAreaWeek { get; }
        public StudyAreaWeekAssessment TargetAssessment { get; }
        public StudyAreaWeekAssessment OtherAssessment { get; }
        public IReadOnlyList<StudyAreaWeek> ConfiguredWeeks { get; set; } = Array.Empty<StudyAreaWeek>();
        public Dictionary<Guid, StudyAreaWeekAssessment?> IndividualAssessments { get; } = [];
        public Dictionary<Guid, IReadOnlyList<StudyRecord>> RecordsByWeek { get; } = [];

        public TestContext()
        {
            DateTime dateTime = WeekStartDate.ToDateTime(TimeOnly.MinValue);
            IsoWeek isoWeek = new(ISOWeek.GetYear(dateTime), ISOWeek.GetWeekOfYear(dateTime));
            WeeklyAssessment = new WeeklyAssessment(isoWeek.WeekNumber, isoWeek.Year, 1500m, 30);
            TargetStudyAreaWeek = new StudyAreaWeek(
                WeekStartDate, Guid.NewGuid(), Guid.NewGuid(), WeeklyAssessment.Id);
            OtherStudyAreaWeek = new StudyAreaWeek(
                WeekStartDate, Guid.NewGuid(), Guid.NewGuid(), WeeklyAssessment.Id);
            TargetAssessment = new StudyAreaWeekAssessment(900m, TargetStudyAreaWeek.Id, 10);
            OtherAssessment = new StudyAreaWeekAssessment(600m, OtherStudyAreaWeek.Id, 20);
            IndividualAssessments[TargetStudyAreaWeek.Id] = TargetAssessment;
            IndividualAssessments[OtherStudyAreaWeek.Id] = OtherAssessment;
            ConfiguredWeeks = [TargetStudyAreaWeek, OtherStudyAreaWeek];
            RecordsByWeek[TargetStudyAreaWeek.Id] =
            [
                new StudyRecord(WeekStartDate, new Minutes(120), TargetStudyAreaWeek.Id),
                new StudyRecord(WeekStartDate.AddDays(1), new Minutes(80), TargetStudyAreaWeek.Id)
            ];
            RecordsByWeek[OtherStudyAreaWeek.Id] =
            [
                new StudyRecord(WeekStartDate, new Minutes(100), OtherStudyAreaWeek.Id),
                new StudyRecord(WeekStartDate.AddDays(2), new Minutes(300), OtherStudyAreaWeek.Id)
            ];

            UnitOfWork.Setup(item => item.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
            IsoWeekCalendar
                .Setup(item => item.GetMonday(WeeklyAssessment.Year, WeeklyAssessment.WeekNumber))
                .Returns(WeekStartDate);
            IsoWeekCalendar
                .Setup(item => item.GetIsoWeek(It.IsAny<DateOnly>()))
                .Returns((DateOnly date) =>
                {
                    DateTime value = date.ToDateTime(TimeOnly.MinValue);
                    return new IsoWeek(ISOWeek.GetYear(value), ISOWeek.GetWeekOfYear(value));
                });
            WeeklyAssessmentRepository
                .Setup(item => item.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .Returns((Guid id, CancellationToken _) => Task.FromResult<WeeklyAssessment?>(
                    id == WeeklyAssessment.Id ? WeeklyAssessment : null));
            StudyAreaWeekRepository
                .Setup(item => item.GetByWeekStartDateAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
                .Returns((DateOnly date, CancellationToken _) => Task.FromResult<IReadOnlyList<StudyAreaWeek>>(
                    date == WeekStartDate ? ConfiguredWeeks : Array.Empty<StudyAreaWeek>()));
            StudyAreaWeekAssessmentRepository
                .Setup(item => item.GetByStudyAreaWeekIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .Returns((Guid id, CancellationToken _) => Task.FromResult<StudyAreaWeekAssessment?>(
                    IndividualAssessments.TryGetValue(id, out StudyAreaWeekAssessment? assessment)
                        ? assessment
                        : null));
            StudyRecordRepository
                .Setup(item => item.GetByStudyAreaWeekIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .Returns((Guid id, CancellationToken _) => Task.FromResult<IReadOnlyList<StudyRecord>>(
                    RecordsByWeek.TryGetValue(id, out IReadOnlyList<StudyRecord>? records)
                        ? records
                        : Array.Empty<StudyRecord>()));
        }

        public WeeklyAssessmentSynchronizer CreateSynchronizer() => new(
            UnitOfWork.Object,
            IsoWeekCalendar.Object,
            StudyRecordRepository.Object,
            StudyAreaWeekRepository.Object,
            WeeklyAssessmentRepository.Object,
            StudyAreaWeekAssessmentRepository.Object);
    }
}
