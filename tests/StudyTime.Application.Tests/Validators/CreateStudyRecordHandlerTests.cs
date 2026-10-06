using Moq;
using StudyTime.Domain.Entities;
using StudyTime.Domain.Exceptions;
using StudyTime.Domain.ValueObjects;
using StudyTime.Domain.Abstractions;
using StudyTime.Domain.Abstractions.Repositories;
using StudyTime.Application.StudyRecords.Commands;

namespace StudyTime.Application.Tests.StudyRecords;

public sealed class CreateStudyRecordHandlerTests
{
    [Fact]
    public async Task Handle_validCurrentWeekConfiguration_createsRecordAndRecalculatesMinutes()
    {
        var weekStart = new DateOnly(2026, 10, 5);
        var studyAreaWeekId = Guid.NewGuid();
        var weeklyAssessmentId = Guid.NewGuid();
        var studyAreaId = Guid.NewGuid();
        var studyPlanId = Guid.NewGuid();

        var studyAreaWeek =
            new StudyAreaWeek(
                studyAreaWeekId,
                weekStart,
                studyAreaId,
                studyPlanId,
                weeklyAssessmentId);

        var individualAssessment =
            new StudyAreaWeekAssessment(
                Guid.NewGuid(),
                1800m,
                0,
                studyAreaWeekId);

        var weeklyAssessment =
            new WeeklyAssessment(
                weeklyAssessmentId,
                41,
                2026,
                1800m,
                0);

        var existingRecord =
            new StudyRecord(
                Guid.NewGuid(),
                weekStart,
                new Minutes(300),
                studyAreaWeekId);

        var repositories =
            CreateRepositories(
                studyAreaWeek,
                individualAssessment,
                weeklyAssessment,
                new[] { studyAreaWeek },
                new[] { existingRecord });

        repositories.CurrentWeekProvider
            .Setup(provider =>
                provider.GetCurrentWeek())
            .Returns(
                new WeekRange(weekStart));

        repositories.IsoWeekCalendar
            .Setup(calendar =>
                calendar.GetIsoWeek(weekStart))
            .Returns(
                new IsoWeek(2026, 41));

        repositories.Clock
            .Setup(clock =>
                clock.UtcNow)
            .Returns(
                new DateTime(
                    2026,
                    10,
                    6,
                    12,
                    0,
                    0,
                    DateTimeKind.Utc));

        StudyRecord? addedRecord = null;

        repositories.StudyRecordRepository
            .Setup(repository =>
                repository.AddAsync(
                    It.IsAny<StudyRecord>(),
                    It.IsAny<CancellationToken>()))
            .Callback<StudyRecord, CancellationToken>(
                (record, _) => addedRecord = record)
            .Returns(Task.CompletedTask);

        repositories.StudyRecordRepository
            .Setup(repository =>
                repository.GetByStudyAreaWeekIdAsync(
                    studyAreaWeekId,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                () =>
                    addedRecord is null
                        ? new List<StudyRecord>
                        {
                            existingRecord
                        }
                        : new List<StudyRecord>
                        {
                            existingRecord,
                            addedRecord
                        });

        var handler =
            new CreateStudyRecordHandler(
                repositories.StudyAreaWeekRepository.Object,
                repositories.StudyAreaWeekAssessmentRepository.Object,
                repositories.WeeklyAssessmentRepository.Object,
                repositories.StudyRecordRepository.Object,
                repositories.CurrentWeekProvider.Object,
                repositories.IsoWeekCalendar.Object,
                repositories.Clock.Object,
                repositories.UnitOfWork.Object);

        var result =
            await handler.Handle(
                new CreateStudyRecordCommand(
                    studyAreaWeekId,
                    120),
                CancellationToken.None);

        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(weekStart.AddDays(1), result.Date);
        Assert.Equal(120, result.Minutes);
        Assert.Equal(studyAreaWeekId, result.StudyAreaWeekId);

        Assert.NotNull(addedRecord);
        Assert.Equal(120, addedRecord!.Minutes.Value);

        Assert.Equal(
            420,
            individualAssessment.MinutesStudied);

        Assert.Equal(
            420,
            weeklyAssessment.MinutesStudied);

        Assert.Equal(
            1800m,
            individualAssessment.WeekIndividualGoal);

        Assert.Equal(
            1800m,
            weeklyAssessment.WeekGlobalGoal);

        repositories.StudyAreaWeekAssessmentRepository.Verify(
            repository =>
                repository.Update(individualAssessment),
            Times.Once);

        repositories.WeeklyAssessmentRepository.Verify(
            repository =>
                repository.Update(weeklyAssessment),
            Times.Once);

        repositories.UnitOfWork.Verify(
            unitOfWork =>
                unitOfWork.SaveChangesAsync(
                    It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_studyAreaWeekFromPreviousWeek_rejectsCreation()
    {
        var currentWeekStart = new DateOnly(2026, 10, 5);
        var previousWeekStart = currentWeekStart.AddDays(-7);

        var studyAreaWeek =
            new StudyAreaWeek(
                previousWeekStart,
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid());

        var repositories =
            CreateRepositories(
                studyAreaWeek,
                null,
                null,
                Array.Empty<StudyAreaWeek>(),
                Array.Empty<StudyRecord>());

        repositories.CurrentWeekProvider
            .Setup(provider =>
                provider.GetCurrentWeek())
            .Returns(
                new WeekRange(currentWeekStart));

        var handler =
            new CreateStudyRecordHandler(
                repositories.StudyAreaWeekRepository.Object,
                repositories.StudyAreaWeekAssessmentRepository.Object,
                repositories.WeeklyAssessmentRepository.Object,
                repositories.StudyRecordRepository.Object,
                repositories.CurrentWeekProvider.Object,
                repositories.IsoWeekCalendar.Object,
                repositories.Clock.Object,
                repositories.UnitOfWork.Object);

        await Assert.ThrowsAsync<Domain.Exceptions.RecordNotInCurrentWeekException>(
            () =>
                handler.Handle(
                    new CreateStudyRecordCommand(
                        studyAreaWeek.Id,
                        60),
                    CancellationToken.None));

        repositories.StudyRecordRepository.Verify(
            repository =>
                repository.AddAsync(
                    It.IsAny<StudyRecord>(),
                    It.IsAny<CancellationToken>()),
            Times.Never);

        repositories.UnitOfWork.Verify(
            unitOfWork =>
                unitOfWork.SaveChangesAsync(
                    It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_missingWeeklyAssessment_rejectsCreationWithoutPersistence()
    {
        var weekStart = new DateOnly(2026, 10, 5);

        var studyAreaWeek =
            new StudyAreaWeek(
                weekStart,
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid());

        var repositories =
            CreateRepositories(
                studyAreaWeek,
                null,
                null,
                new[] { studyAreaWeek },
                Array.Empty<StudyRecord>());

        repositories.CurrentWeekProvider
            .Setup(provider =>
                provider.GetCurrentWeek())
            .Returns(
                new WeekRange(weekStart));

        repositories.IsoWeekCalendar
            .Setup(calendar =>
                calendar.GetIsoWeek(weekStart))
            .Returns(
                new IsoWeek(2026, 41));

        repositories.WeeklyAssessmentRepository
            .Setup(repository =>
                repository.GetByYearAndWeekNumberAsync(
                    2026,
                    41,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                (WeeklyAssessment?)null);

        var handler =
            new CreateStudyRecordHandler(
                repositories.StudyAreaWeekRepository.Object,
                repositories.StudyAreaWeekAssessmentRepository.Object,
                repositories.WeeklyAssessmentRepository.Object,
                repositories.StudyRecordRepository.Object,
                repositories.CurrentWeekProvider.Object,
                repositories.IsoWeekCalendar.Object,
                repositories.Clock.Object,
                repositories.UnitOfWork.Object);

        await Assert.ThrowsAsync<DomainException>(
            () =>
                handler.Handle(
                    new CreateStudyRecordCommand(
                        studyAreaWeek.Id,
                        60),
                    CancellationToken.None));

        repositories.StudyRecordRepository.Verify(
            repository =>
                repository.AddAsync(
                    It.IsAny<StudyRecord>(),
                    It.IsAny<CancellationToken>()),
            Times.Never);

        repositories.UnitOfWork.Verify(
            unitOfWork =>
                unitOfWork.SaveChangesAsync(
                    It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static RepositorySet CreateRepositories(
        StudyAreaWeek studyAreaWeek,
        StudyAreaWeekAssessment? individualAssessment,
        WeeklyAssessment? weeklyAssessment,
        IReadOnlyList<StudyAreaWeek> studyAreaWeeks,
        IReadOnlyList<StudyRecord> records)
    {
        var studyAreaWeekRepository =
            new Mock<IStudyAreaWeekRepository>();

        var studyAreaWeekAssessmentRepository =
            new Mock<IStudyAreaWeekAssessmentRepository>();

        var weeklyAssessmentRepository =
            new Mock<IWeeklyAssessmentRepository>();

        var studyRecordRepository =
            new Mock<IStudyRecordRepository>();

        var currentWeekProvider =
            new Mock<ICurrentWeekProvider>();

        var isoWeekCalendar =
            new Mock<IIsoWeekCalendar>();

        var clock =
            new Mock<IClock>();

        var unitOfWork =
            new Mock<IUnitOfWork>();

        studyAreaWeekRepository
            .Setup(repository =>
                repository.GetByIdAsync(
                    studyAreaWeek.Id,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(studyAreaWeek);

        studyAreaWeekRepository
            .Setup(repository =>
                repository.GetByWeekStartDateAsync(
                    studyAreaWeek.WeekStartDate,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(studyAreaWeeks);

        studyAreaWeekAssessmentRepository
            .Setup(repository =>
                repository.GetByStudyAreaWeekIdAsync(
                    studyAreaWeek.Id,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(individualAssessment);

        if (weeklyAssessment is not null)
        {
            weeklyAssessmentRepository
                .Setup(repository =>
                    repository.GetByYearAndWeekNumberAsync(
                        weeklyAssessment.Year,
                        weeklyAssessment.WeekNumber,
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(weeklyAssessment);
        }

        studyRecordRepository
            .Setup(repository =>
                repository.GetByStudyAreaWeekIdAsync(
                    studyAreaWeek.Id,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(records);

        return new RepositorySet(
            studyAreaWeekRepository,
            studyAreaWeekAssessmentRepository,
            weeklyAssessmentRepository,
            studyRecordRepository,
            currentWeekProvider,
            isoWeekCalendar,
            clock,
            unitOfWork);
    }

    private sealed record RepositorySet(
        Mock<IStudyAreaWeekRepository> StudyAreaWeekRepository,
        Mock<IStudyAreaWeekAssessmentRepository> StudyAreaWeekAssessmentRepository,
        Mock<IWeeklyAssessmentRepository> WeeklyAssessmentRepository,
        Mock<IStudyRecordRepository> StudyRecordRepository,
        Mock<ICurrentWeekProvider> CurrentWeekProvider,
        Mock<IIsoWeekCalendar> IsoWeekCalendar,
        Mock<IClock> Clock,
        Mock<IUnitOfWork> UnitOfWork);
}