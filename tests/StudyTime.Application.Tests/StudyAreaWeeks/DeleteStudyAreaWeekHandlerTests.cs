using Moq;
using StudyTime.Application.StudyAreaWeeks.Commands;
using StudyTime.Domain.Abstractions;
using StudyTime.Domain.Abstractions.Repositories;
using StudyTime.Domain.Entities;
using StudyTime.Domain.Exceptions;
using StudyTime.Domain.ValueObjects;

namespace StudyTime.Application.Tests.StudyAreaWeeks;

public sealed class DeleteStudyAreaWeekHandlerTests
{
    [Fact]
    public async Task Handle_OnlyStudyAreaWeek_RemovesConfigurationAssessmentRecordsAndWeeklyAssessment()
    {
        var weekStartDate = new DateOnly(2026, 9, 28);
        var studyAreaWeekId = Guid.NewGuid();
        var studyAreaId = Guid.NewGuid();
        var studyPlanId = Guid.NewGuid();
        var weeklyAssessmentId = Guid.NewGuid();

        var studyAreaWeek = new StudyAreaWeek(
            studyAreaWeekId,
            weekStartDate,
            studyAreaId,
            studyPlanId,
            weeklyAssessmentId);

        var assessment = new StudyAreaWeekAssessment(
            Guid.NewGuid(),
            1500m,
            120,
            studyAreaWeekId);

        var record = new StudyRecord(
            DateOnly.FromDateTime(weekStartDate.ToDateTime(TimeOnly.MinValue)),
            new Minutes(120),
            studyAreaWeekId);

        var weeklyAssessment = new WeeklyAssessment(
            weeklyAssessmentId,
            40,
            2026,
            1500m,
            1500);

        var studyAreaWeekRepository = new Mock<IStudyAreaWeekRepository>();
        var assessmentRepository = new Mock<IStudyAreaWeekAssessmentRepository>();
        var recordRepository = new Mock<IStudyRecordRepository>();
        var weeklyAssessmentRepository = new Mock<IWeeklyAssessmentRepository>();
        var currentWeekProvider = new Mock<ICurrentWeekProvider>();
        var isoWeekCalendar = new Mock<IIsoWeekCalendar>();
        var unitOfWork = new Mock<IUnitOfWork>();

        studyAreaWeekRepository
            .Setup(x => x.GetByIdAsync(studyAreaWeekId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(studyAreaWeek);

        studyAreaWeekRepository
            .Setup(x => x.GetByWeekStartDateAsync(weekStartDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { studyAreaWeek });

        assessmentRepository
            .Setup(x => x.GetByStudyAreaWeekIdAsync(studyAreaWeekId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assessment);

        recordRepository
            .Setup(x => x.GetByStudyAreaWeekIdAsync(studyAreaWeekId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { record });

        weeklyAssessmentRepository
            .Setup(x => x.GetByYearAndWeekNumberAsync(2026, 40, It.IsAny<CancellationToken>()))
            .ReturnsAsync(weeklyAssessment);

        currentWeekProvider
            .Setup(x => x.GetCurrentWeek())
            .Returns(new WeekRange(weekStartDate));

        isoWeekCalendar
            .Setup(x => x.GetIsoWeek(weekStartDate))
            .Returns(new IsoWeek(2026, 40));

        unitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(4);

        var handler = CreateHandler(
            studyAreaWeekRepository,
            assessmentRepository,
            recordRepository,
            weeklyAssessmentRepository,
            currentWeekProvider,
            isoWeekCalendar,
            unitOfWork);

        await handler.Handle(
            new DeleteStudyAreaWeekCommand(studyAreaWeekId),
            CancellationToken.None);

        recordRepository.Verify(x => x.Delete(record), Times.Once);

        assessmentRepository.Verify(x => x.Delete(assessment), Times.Once);

        studyAreaWeekRepository.Verify(x => x.Delete(studyAreaWeek), Times.Once);

        weeklyAssessmentRepository.Verify(x => x.Delete(weeklyAssessment), Times.Once);

        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_RemovingOneOfSeveralStudyAreaWeeks_RecalculatesWeeklyAssessment()
    {
        var weekStartDate = new DateOnly(2026, 9, 28);
        var studyAreaWeekIdToDelete = Guid.NewGuid();
        var remainingStudyAreaWeekId = Guid.NewGuid();
        var weeklyAssessmentId = Guid.NewGuid();

        var deletedStudyAreaWeek = new StudyAreaWeek(
            studyAreaWeekIdToDelete,
            weekStartDate,
            Guid.NewGuid(),
            Guid.NewGuid(),
            weeklyAssessmentId);

        var remainingStudyAreaWeek = new StudyAreaWeek(
            remainingStudyAreaWeekId,
            weekStartDate,
            Guid.NewGuid(),
            Guid.NewGuid(),
            weeklyAssessmentId);

        var deletedAssessment = new StudyAreaWeekAssessment(
            Guid.NewGuid(),
            1000m,
            300,
            studyAreaWeekIdToDelete);

        var remainingAssessment = new StudyAreaWeekAssessment(
            Guid.NewGuid(),
            1600m,
            500,
            remainingStudyAreaWeekId);

        var weeklyAssessment = new WeeklyAssessment(
            weeklyAssessmentId,
            40,
            2026,
            2600m,
            2600);

        var studyAreaWeekRepository = new Mock<IStudyAreaWeekRepository>();
        var assessmentRepository = new Mock<IStudyAreaWeekAssessmentRepository>();
        var recordRepository = new Mock<IStudyRecordRepository>();
        var weeklyAssessmentRepository = new Mock<IWeeklyAssessmentRepository>();
        var currentWeekProvider = new Mock<ICurrentWeekProvider>();
        var isoWeekCalendar = new Mock<IIsoWeekCalendar>();
        var unitOfWork = new Mock<IUnitOfWork>();

        studyAreaWeekRepository
            .Setup(x => x.GetByIdAsync(
                studyAreaWeekIdToDelete,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(deletedStudyAreaWeek);

        studyAreaWeekRepository
            .Setup(x => x.GetByWeekStartDateAsync(
                weekStartDate,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new[]
                {
                    deletedStudyAreaWeek,
                    remainingStudyAreaWeek
                });

        assessmentRepository
            .Setup(x => x.GetByStudyAreaWeekIdAsync(
                studyAreaWeekIdToDelete,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(deletedAssessment);

        assessmentRepository
            .Setup(x => x.GetByStudyAreaWeekIdAsync(
                remainingStudyAreaWeekId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(remainingAssessment);

        recordRepository
            .Setup(x => x.GetByStudyAreaWeekIdAsync(
                studyAreaWeekIdToDelete,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<StudyRecord>());

        recordRepository
            .Setup(x => x.GetByStudyAreaWeekIdAsync(
                remainingStudyAreaWeekId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new[]
                {
                    new StudyRecord(
                        DateOnly.FromDateTime(
                            weekStartDate.ToDateTime(TimeOnly.MinValue)),
                        new Minutes(500),
                        remainingStudyAreaWeekId)
                });

        weeklyAssessmentRepository
            .Setup(x => x.GetByYearAndWeekNumberAsync(
                2026,
                40,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(weeklyAssessment);

        currentWeekProvider
            .Setup(x => x.GetCurrentWeek())
            .Returns(new WeekRange(weekStartDate));

        isoWeekCalendar
            .Setup(x => x.GetIsoWeek(weekStartDate))
            .Returns(new IsoWeek(2026, 40));

        unitOfWork
            .Setup(x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        var handler = CreateHandler(
            studyAreaWeekRepository,
            assessmentRepository,
            recordRepository,
            weeklyAssessmentRepository,
            currentWeekProvider,
            isoWeekCalendar,
            unitOfWork);

        await handler.Handle(
            new DeleteStudyAreaWeekCommand(studyAreaWeekIdToDelete),
            CancellationToken.None);

        Assert.Equal(1600m, weeklyAssessment.WeekGlobalGoal);
        Assert.Equal(500, weeklyAssessment.MinutesStudied);

        assessmentRepository.Verify(
            x => x.Update(remainingAssessment),
            Times.Once);

        weeklyAssessmentRepository.Verify(
            x => x.Update(weeklyAssessment),
            Times.Once);

        weeklyAssessmentRepository.Verify(
            x => x.Delete(It.IsAny<WeeklyAssessment>()),
            Times.Never);

        unitOfWork.Verify(
            x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_EmptyId_ThrowsArgumentException()
    {
        var handler = CreateHandler();

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => handler.Handle(
                new DeleteStudyAreaWeekCommand(Guid.Empty),
                CancellationToken.None));

        Assert.Equal("Id", exception.ParamName);
    }

    [Fact]
    public async Task Handle_StudyAreaWeekDoesNotExist_ThrowsKeyNotFoundException()
    {
        var studyAreaWeekRepository =
            new Mock<IStudyAreaWeekRepository>();

        studyAreaWeekRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((StudyAreaWeek?)null);

        var handler = CreateHandler(studyAreaWeekRepository: studyAreaWeekRepository);

        var id = Guid.NewGuid();

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => handler.Handle(
                new DeleteStudyAreaWeekCommand(id),
                CancellationToken.None));
    }

    [Fact]
    public async Task Handle_CurrentWeekGlobalGoalNotReached_ThrowsWeekConfigurationLockedException()
    {
        var weekStartDate = new DateOnly(2026, 9, 28);
        var studyAreaWeekId = Guid.NewGuid();
        var weeklyAssessmentId = Guid.NewGuid();

        var studyAreaWeek = new StudyAreaWeek(
            studyAreaWeekId,
            weekStartDate,
            Guid.NewGuid(),
            Guid.NewGuid(),
            weeklyAssessmentId);

        var weeklyAssessment = new WeeklyAssessment(weeklyAssessmentId, 40, 2026, 1500m, 1499);
        var studyAreaWeekRepository = new Mock<IStudyAreaWeekRepository>();
        var assessmentRepository = new Mock<IStudyAreaWeekAssessmentRepository>();
        var recordRepository = new Mock<IStudyRecordRepository>();
        var weeklyAssessmentRepository = new Mock<IWeeklyAssessmentRepository>();
        var currentWeekProvider = new Mock<ICurrentWeekProvider>();
        var isoWeekCalendar = new Mock<IIsoWeekCalendar>();
        var unitOfWork = new Mock<IUnitOfWork>();

        studyAreaWeekRepository
            .Setup(x => x.GetByIdAsync(
                studyAreaWeekId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(studyAreaWeek);

        weeklyAssessmentRepository
            .Setup(x => x.GetByYearAndWeekNumberAsync(
                2026,
                40,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(weeklyAssessment);

        currentWeekProvider
            .Setup(x => x.GetCurrentWeek())
            .Returns(new WeekRange(weekStartDate));

        isoWeekCalendar
            .Setup(x => x.GetIsoWeek(weekStartDate))
            .Returns(new IsoWeek(2026, 40));

        var handler = CreateHandler(
            studyAreaWeekRepository,
            assessmentRepository,
            recordRepository,
            weeklyAssessmentRepository,
            currentWeekProvider,
            isoWeekCalendar,
            unitOfWork);

        await Assert.ThrowsAsync<WeekConfigurationLockedException>(
            () => handler.Handle(
                new DeleteStudyAreaWeekCommand(studyAreaWeekId),
                CancellationToken.None));

        studyAreaWeekRepository.Verify(x => x.Delete(It.IsAny<StudyAreaWeek>()), Times.Never);
        assessmentRepository.Verify(x => x.Delete(It.IsAny<StudyAreaWeekAssessment>()), Times.Never);
        recordRepository.Verify(x => x.Delete(It.IsAny<StudyRecord>()), Times.Never);
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private static DeleteStudyAreaWeekHandler CreateHandler(
        Mock<IStudyAreaWeekRepository>? studyAreaWeekRepository = null,
        Mock<IStudyAreaWeekAssessmentRepository>? assessmentRepository = null,
        Mock<IStudyRecordRepository>? recordRepository = null,
        Mock<IWeeklyAssessmentRepository>? weeklyAssessmentRepository = null,
        Mock<ICurrentWeekProvider>? currentWeekProvider = null,
        Mock<IIsoWeekCalendar>? isoWeekCalendar = null,
        Mock<IUnitOfWork>? unitOfWork = null)
    {
        return new DeleteStudyAreaWeekHandler(
            (studyAreaWeekRepository ?? new Mock<IStudyAreaWeekRepository>()).Object,
            (assessmentRepository ?? new Mock<IStudyAreaWeekAssessmentRepository>()).Object,
            (recordRepository ?? new Mock<IStudyRecordRepository>()).Object,
            (weeklyAssessmentRepository ?? new Mock<IWeeklyAssessmentRepository>()).Object,
            (currentWeekProvider ?? new Mock<ICurrentWeekProvider>()).Object,
            (isoWeekCalendar ?? new Mock<IIsoWeekCalendar>()).Object,
            (unitOfWork ?? new Mock<IUnitOfWork>()).Object);
    }
}