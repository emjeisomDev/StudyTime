using Moq;
using StudyTime.Domain.Entities;
using StudyTime.Domain.Exceptions;
using StudyTime.Domain.ValueObjects;
using StudyTime.Domain.Abstractions;
using StudyTime.Domain.Abstractions.Repositories;
using StudyTime.Application.WeeklyAssessments.Services;

namespace StudyTime.Application.Tests.WeeklyAssessments;

public sealed class WeeklyAssessmentSynchronizerTests
{
    [Fact]
    public async Task SynchronizeAsync_recalculatesMinutesAndGlobalGoal_successfully()
    {
        var weeklyAssessmentId = Guid.NewGuid();
        var studyAreaWeekId = Guid.NewGuid();

        var weeklyAssessment = new WeeklyAssessment(
            weeklyAssessmentId,
            10,
            2026,
            1500m);

        var weekStartDate = new DateOnly(2026, 3, 2);

        var studyAreaWeek =
            new StudyAreaWeek(
                studyAreaWeekId,
                weekStartDate,
                Guid.NewGuid(),
                Guid.NewGuid(),
                weeklyAssessmentId);

        var individualAssessment = new StudyAreaWeekAssessment(1500m, studyAreaWeekId);
        var weeklyAssessmentRepository = new Mock<IWeeklyAssessmentRepository>();
        var studyAreaWeekRepository = new Mock<IStudyAreaWeekRepository>();
        var studyAreaWeekAssessmentRepository = new Mock<IStudyAreaWeekAssessmentRepository>();
        var studyRecordRepository = new Mock<IStudyRecordRepository>();
        var isoWeekCalendar = new Mock<IIsoWeekCalendar>();
        var unitOfWork = new Mock<IUnitOfWork>();

        weeklyAssessmentRepository
            .Setup(repository =>
                repository.GetByIdAsync(
                    weeklyAssessmentId,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(weeklyAssessment);

        studyAreaWeekRepository
            .Setup(repository =>
                repository.GetByWeekStartDateAsync(
                    weekStartDate,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new List<StudyAreaWeek>
                {
                    studyAreaWeek
                });

        studyAreaWeekAssessmentRepository
            .Setup(repository =>
                repository.GetByStudyAreaWeekIdAsync(
                    studyAreaWeekId,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(individualAssessment);

        studyRecordRepository
            .Setup(repository =>
                repository.GetByStudyAreaWeekIdAsync(
                    studyAreaWeekId,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<StudyRecord>());

        isoWeekCalendar
            .Setup(calendar =>
                calendar.GetMonday(2026, 10))
            .Returns(weekStartDate);

        isoWeekCalendar
            .Setup(calendar =>
                calendar.GetIsoWeek(weekStartDate))
            .Returns(new IsoWeek(2026, 10));

        unitOfWork
            .Setup(work =>
                work.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        var synchronizer = new WeeklyAssessmentSynchronizer(
                weeklyAssessmentRepository.Object,
                studyAreaWeekRepository.Object,
                studyAreaWeekAssessmentRepository.Object,
                studyRecordRepository.Object,
                isoWeekCalendar.Object,
                unitOfWork.Object);

        await synchronizer.SynchronizeAsync(weeklyAssessmentId);

        Assert.Equal(0, individualAssessment.MinutesStudied);
        Assert.Equal(0, weeklyAssessment.MinutesStudied);
        Assert.Equal(1500m, weeklyAssessment.WeekGlobalGoal);

        studyAreaWeekAssessmentRepository.Verify(
            repository => repository.Update(individualAssessment),
            Times.Once);

        weeklyAssessmentRepository.Verify(
            repository => repository.Update(weeklyAssessment),
            Times.Once);

        unitOfWork.Verify(
            work => work.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SynchronizeAsync_rejectsEmptyWeeklyAssessmentId()
    {
        var weeklyAssessmentRepository = new Mock<IWeeklyAssessmentRepository>();
        var studyAreaWeekRepository = new Mock<IStudyAreaWeekRepository>();
        var studyAreaWeekAssessmentRepository = new Mock<IStudyAreaWeekAssessmentRepository>();
        var studyRecordRepository = new Mock<IStudyRecordRepository>();
        var isoWeekCalendar = new Mock<IIsoWeekCalendar>();
        var unitOfWork = new Mock<IUnitOfWork>();

        var synchronizer = new WeeklyAssessmentSynchronizer(
                weeklyAssessmentRepository.Object,
                studyAreaWeekRepository.Object,
                studyAreaWeekAssessmentRepository.Object,
                studyRecordRepository.Object,
                isoWeekCalendar.Object,
                unitOfWork.Object);

        await Assert.ThrowsAsync<ArgumentException>(() => synchronizer.SynchronizeAsync(Guid.Empty));

        weeklyAssessmentRepository.Verify(
            repository =>
                repository.GetByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
            Times.Never);

        unitOfWork.Verify(
            work => work.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SynchronizeAsync_rejectsMissingWeeklyAssessment()
    {
        var weeklyAssessmentId = Guid.NewGuid();
        var weeklyAssessmentRepository = new Mock<IWeeklyAssessmentRepository>();
        var studyAreaWeekRepository = new Mock<IStudyAreaWeekRepository>();
        var studyAreaWeekAssessmentRepository = new Mock<IStudyAreaWeekAssessmentRepository>();
        var studyRecordRepository = new Mock<IStudyRecordRepository>();
        var isoWeekCalendar = new Mock<IIsoWeekCalendar>();
        var unitOfWork = new Mock<IUnitOfWork>();

        weeklyAssessmentRepository.Setup(repository =>
                repository.GetByIdAsync(
                    weeklyAssessmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((WeeklyAssessment?)null);

        var synchronizer = new WeeklyAssessmentSynchronizer(
                weeklyAssessmentRepository.Object,
                studyAreaWeekRepository.Object,
                studyAreaWeekAssessmentRepository.Object,
                studyRecordRepository.Object,
                isoWeekCalendar.Object,
                unitOfWork.Object);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => synchronizer.SynchronizeAsync(weeklyAssessmentId));

        unitOfWork.Verify(
            work => work.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SynchronizeAsync_rejectsWeeklyAssessmentWithoutStudyAreaWeeks()
    {
        var weeklyAssessmentId = Guid.NewGuid();

        var weeklyAssessment = new WeeklyAssessment(
            weeklyAssessmentId,
            10,
            2026,
            1500m);

        var weekStartDate = new DateOnly(2026, 3, 2);
        var weeklyAssessmentRepository = new Mock<IWeeklyAssessmentRepository>();
        var studyAreaWeekRepository = new Mock<IStudyAreaWeekRepository>();
        var studyAreaWeekAssessmentRepository = new Mock<IStudyAreaWeekAssessmentRepository>();
        var studyRecordRepository = new Mock<IStudyRecordRepository>();
        var isoWeekCalendar = new Mock<IIsoWeekCalendar>();
        var unitOfWork = new Mock<IUnitOfWork>();

        weeklyAssessmentRepository
            .Setup(repository =>
                repository.GetByIdAsync(
                    weeklyAssessmentId,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(weeklyAssessment);

        isoWeekCalendar
            .Setup(calendar =>
                calendar.GetMonday(2026, 10))
            .Returns(weekStartDate);

        isoWeekCalendar
            .Setup(calendar =>
                calendar.GetIsoWeek(weekStartDate))
            .Returns(new IsoWeek(2026, 10));

        studyAreaWeekRepository
            .Setup(repository =>
                repository.GetByWeekStartDateAsync(
                    weekStartDate,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<StudyAreaWeek>());

        var synchronizer = new WeeklyAssessmentSynchronizer(
                weeklyAssessmentRepository.Object,
                studyAreaWeekRepository.Object,
                studyAreaWeekAssessmentRepository.Object,
                studyRecordRepository.Object,
                isoWeekCalendar.Object,
                unitOfWork.Object);

        await Assert.ThrowsAsync<DomainException>(() => synchronizer.SynchronizeAsync(weeklyAssessmentId));

        unitOfWork.Verify(
            work => work.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SynchronizeAsync_rejectsStudyAreaWeekFromAnotherWeeklyAssessment()
    {
        var weeklyAssessmentId = Guid.NewGuid();
        var anotherWeeklyAssessmentId = Guid.NewGuid();

        var weeklyAssessment = new WeeklyAssessment(
            weeklyAssessmentId,
            10,
            2026,
            1500m);

        var weekStartDate = new DateOnly(2026, 3, 2);

        var studyAreaWeek =
            new StudyAreaWeek(
                Guid.NewGuid(),
                weekStartDate,
                Guid.NewGuid(),
                Guid.NewGuid(),
                anotherWeeklyAssessmentId);

        var weeklyAssessmentRepository = new Mock<IWeeklyAssessmentRepository>();
        var studyAreaWeekRepository = new Mock<IStudyAreaWeekRepository>();
        var studyAreaWeekAssessmentRepository = new Mock<IStudyAreaWeekAssessmentRepository>();
        var studyRecordRepository = new Mock<IStudyRecordRepository>();
        var isoWeekCalendar = new Mock<IIsoWeekCalendar>();
        var unitOfWork = new Mock<IUnitOfWork>();

        weeklyAssessmentRepository
            .Setup(repository =>
                repository.GetByIdAsync(
                    weeklyAssessmentId,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(weeklyAssessment);

        isoWeekCalendar
            .Setup(calendar =>
                calendar.GetMonday(2026, 10))
            .Returns(weekStartDate);

        isoWeekCalendar
            .Setup(calendar =>
                calendar.GetIsoWeek(weekStartDate))
            .Returns(new IsoWeek(2026, 10));

        studyAreaWeekRepository
            .Setup(repository =>
                repository.GetByWeekStartDateAsync(
                    weekStartDate,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new List<StudyAreaWeek>
                {
                    studyAreaWeek
                });

        var synchronizer =
            new WeeklyAssessmentSynchronizer(
                weeklyAssessmentRepository.Object,
                studyAreaWeekRepository.Object,
                studyAreaWeekAssessmentRepository.Object,
                studyRecordRepository.Object,
                isoWeekCalendar.Object,
                unitOfWork.Object);

        await Assert.ThrowsAsync<DomainException>(() => synchronizer.SynchronizeAsync(weeklyAssessmentId));

        studyAreaWeekAssessmentRepository.Verify(
            repository =>
                repository.GetByStudyAreaWeekIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
            Times.Never);

        unitOfWork.Verify(
            work => work.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SynchronizeAsync_rejectsMissingIndividualAssessment()
    {
        var weeklyAssessmentId = Guid.NewGuid();
        var studyAreaWeekId = Guid.NewGuid();

        var weeklyAssessment = new WeeklyAssessment(
            weeklyAssessmentId,
            10,
            2026,
            1500m);

        var weekStartDate = new DateOnly(2026, 3, 2);

        var studyAreaWeek =
            new StudyAreaWeek(
                studyAreaWeekId,
                weekStartDate,
                Guid.NewGuid(),
                Guid.NewGuid(),
                weeklyAssessmentId);

        var weeklyAssessmentRepository = new Mock<IWeeklyAssessmentRepository>();
        var studyAreaWeekRepository = new Mock<IStudyAreaWeekRepository>();
        var studyAreaWeekAssessmentRepository = new Mock<IStudyAreaWeekAssessmentRepository>();
        var studyRecordRepository = new Mock<IStudyRecordRepository>();
        var isoWeekCalendar = new Mock<IIsoWeekCalendar>();
        var unitOfWork = new Mock<IUnitOfWork>();

        weeklyAssessmentRepository
            .Setup(repository =>
                repository.GetByIdAsync(
                    weeklyAssessmentId,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(weeklyAssessment);

        isoWeekCalendar
            .Setup(calendar => calendar.GetMonday(2026, 10))
            .Returns(weekStartDate);

        isoWeekCalendar
            .Setup(calendar => calendar.GetIsoWeek(weekStartDate))
            .Returns(new IsoWeek(2026, 10));

        studyAreaWeekRepository
            .Setup(repository =>
                repository.GetByWeekStartDateAsync(
                    weekStartDate,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new List<StudyAreaWeek>
                {
                    studyAreaWeek
                });

        studyAreaWeekAssessmentRepository
            .Setup(repository => repository.GetByStudyAreaWeekIdAsync(studyAreaWeekId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((StudyAreaWeekAssessment?)null);

        var synchronizer =
            new WeeklyAssessmentSynchronizer(
                weeklyAssessmentRepository.Object,
                studyAreaWeekRepository.Object,
                studyAreaWeekAssessmentRepository.Object,
                studyRecordRepository.Object,
                isoWeekCalendar.Object,
                unitOfWork.Object);

        await Assert.ThrowsAsync<DomainException>(
            () => synchronizer.SynchronizeAsync(weeklyAssessmentId));

        unitOfWork.Verify(
            work => work.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }
}