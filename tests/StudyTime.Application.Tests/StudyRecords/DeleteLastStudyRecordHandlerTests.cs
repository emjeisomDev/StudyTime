using Moq;
using StudyTime.Application.StudyRecords.Commands;
using StudyTime.Domain.Abstractions;
using StudyTime.Domain.Abstractions.Repositories;
using StudyTime.Domain.Entities;
using StudyTime.Domain.Exceptions;
using StudyTime.Domain.ValueObjects;

namespace StudyTime.Application.Tests.StudyRecords;

public sealed class DeleteLastStudyRecordHandlerTests
{
    [Fact]
    public async Task Handle_QuandoExisteRegistroElegivel_RemoveUltimoRegistroERecalculaAvaliacoes()
    {
        var studyAreaWeekId = Guid.NewGuid();
        var weeklyAssessmentId = Guid.NewGuid();
        var firstRecordId = Guid.NewGuid();
        var lastRecordId = Guid.NewGuid();

        var weekStartDate = new DateOnly(2026, 10, 5);
        var weekNumber = System.Globalization.ISOWeek.GetWeekOfYear(
            weekStartDate.ToDateTime(TimeOnly.MinValue));
        var year = System.Globalization.ISOWeek.GetYear(
            weekStartDate.ToDateTime(TimeOnly.MinValue));

        var studyAreaWeek = new StudyAreaWeek(
            studyAreaWeekId,
            weekStartDate,
            Guid.NewGuid(),
            Guid.NewGuid(),
            weeklyAssessmentId);

        var weeklyAssessment = new WeeklyAssessment(
            weeklyAssessmentId,
            weekNumber,
            year,
            1800m,
            900);

        var studyAreaWeekAssessment = new StudyAreaWeekAssessment(
            Guid.NewGuid(),
            1800m,
            900,
            studyAreaWeekId);

        var firstRecord = new StudyRecord(
            firstRecordId,
            weekStartDate,
            new Minutes(300),
            studyAreaWeekId);

        var lastRecord = new StudyRecord(
            lastRecordId,
            weekStartDate.AddDays(1),
            new Minutes(600),
            studyAreaWeekId);

        var remainingRecords = new List<StudyRecord>
        {
            firstRecord
        };

        var studyAreaWeekRepository =
            new Mock<IStudyAreaWeekRepository>();

        studyAreaWeekRepository
            .Setup(repository =>
                repository.GetByIdAsync(
                    studyAreaWeekId,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(studyAreaWeek);

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

        var studyAreaWeekAssessmentRepository =
            new Mock<IStudyAreaWeekAssessmentRepository>();

        studyAreaWeekAssessmentRepository
            .Setup(repository =>
                repository.GetByStudyAreaWeekIdAsync(
                    studyAreaWeekId,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(studyAreaWeekAssessment);

        var weeklyAssessmentRepository =
            new Mock<IWeeklyAssessmentRepository>();

        weeklyAssessmentRepository
            .Setup(repository =>
                repository.GetByYearAndWeekNumberAsync(
                    year,
                    weekNumber,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(weeklyAssessment);

        var studyRecordRepository =
            new Mock<IStudyRecordRepository>();

        studyRecordRepository
            .Setup(repository =>
                repository.GetByStudyAreaWeekIdAsync(
                    studyAreaWeekId,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                () => remainingRecords);

        var lifoSelector =
            new Mock<IStudyRecordLifoSelector>();

        lifoSelector
            .Setup(selector =>
                selector.SelectLastEligibleAsync(
                    studyAreaWeekId,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(lastRecord);

        var currentWeekProvider =
            new Mock<ICurrentWeekProvider>();

        currentWeekProvider
            .Setup(provider => provider.GetCurrentWeek())
            .Returns(new WeekRange(weekStartDate));

        var isoWeekCalendar =
            new Mock<IIsoWeekCalendar>();

        isoWeekCalendar
            .Setup(calendar =>
                calendar.GetIsoWeek(weekStartDate))
            .Returns(new IsoWeek(year, weekNumber));

        var unitOfWork =
            new Mock<IUnitOfWork>();

        unitOfWork
            .Setup(work => work.SaveChangesAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = new DeleteLastStudyRecordHandler(
            studyAreaWeekRepository.Object,
            studyAreaWeekAssessmentRepository.Object,
            weeklyAssessmentRepository.Object,
            studyRecordRepository.Object,
            lifoSelector.Object,
            currentWeekProvider.Object,
            isoWeekCalendar.Object,
            unitOfWork.Object);

        await handler.Handle(
            new DeleteLastStudyRecordCommand(studyAreaWeekId),
            CancellationToken.None);

        studyRecordRepository.Verify(
            repository => repository.Delete(lastRecord),
            Times.Once);

        Assert.Equal(300, studyAreaWeekAssessment.MinutesStudied);
        Assert.Equal(300, weeklyAssessment.MinutesStudied);

        Assert.Equal(
            1800m,
            studyAreaWeekAssessment.WeekIndividualGoal);

        Assert.Equal(
            1800m,
            weeklyAssessment.WeekGlobalGoal);

        studyAreaWeekAssessmentRepository.Verify(
            repository => repository.Update(studyAreaWeekAssessment),
            Times.Once);

        weeklyAssessmentRepository.Verify(
            repository => repository.Update(weeklyAssessment),
            Times.Once);

        unitOfWork.Verify(
            work => work.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_QuandoNaoExisteRegistroElegivel_LancaNoEligibleStudyRecordExceptionENaoPersiste()
    {
        var studyAreaWeekId = Guid.NewGuid();
        var weeklyAssessmentId = Guid.NewGuid();
        var weekStartDate = new DateOnly(2026, 10, 5);

        var isoWeek = System.Globalization.ISOWeek.GetWeekOfYear(
            weekStartDate.ToDateTime(TimeOnly.MinValue));

        var isoYear = System.Globalization.ISOWeek.GetYear(
            weekStartDate.ToDateTime(TimeOnly.MinValue));

        var studyAreaWeek = new StudyAreaWeek(
            studyAreaWeekId,
            weekStartDate,
            Guid.NewGuid(),
            Guid.NewGuid(),
            weeklyAssessmentId);

        var weeklyAssessment = new WeeklyAssessment(
            weeklyAssessmentId,
            isoWeek,
            isoYear,
            1800m,
            0);

        var assessment = new StudyAreaWeekAssessment(
            1800m,
            studyAreaWeekId);

        var studyAreaWeekRepository =
            new Mock<IStudyAreaWeekRepository>();

        studyAreaWeekRepository
            .Setup(repository =>
                repository.GetByIdAsync(
                    studyAreaWeekId,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(studyAreaWeek);

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

        var assessmentRepository =
            new Mock<IStudyAreaWeekAssessmentRepository>();

        assessmentRepository
            .Setup(repository =>
                repository.GetByStudyAreaWeekIdAsync(
                    studyAreaWeekId,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(assessment);

        var weeklyAssessmentRepository =
            new Mock<IWeeklyAssessmentRepository>();

        weeklyAssessmentRepository
            .Setup(repository =>
                repository.GetByYearAndWeekNumberAsync(
                    isoYear,
                    isoWeek,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(weeklyAssessment);

        var studyRecordRepository =
            new Mock<IStudyRecordRepository>();

        var lifoSelector =
            new Mock<IStudyRecordLifoSelector>();

        lifoSelector
            .Setup(selector =>
                selector.SelectLastEligibleAsync(
                    studyAreaWeekId,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync((StudyRecord?)null);

        var currentWeekProvider =
            new Mock<ICurrentWeekProvider>();

        currentWeekProvider
            .Setup(provider => provider.GetCurrentWeek())
            .Returns(new WeekRange(weekStartDate));

        var isoWeekCalendar =
            new Mock<IIsoWeekCalendar>();

        isoWeekCalendar
            .Setup(calendar =>
                calendar.GetIsoWeek(weekStartDate))
            .Returns(new IsoWeek(isoYear, isoWeek));

        var unitOfWork =
            new Mock<IUnitOfWork>();

        var handler = new DeleteLastStudyRecordHandler(
            studyAreaWeekRepository.Object,
            assessmentRepository.Object,
            weeklyAssessmentRepository.Object,
            studyRecordRepository.Object,
            lifoSelector.Object,
            currentWeekProvider.Object,
            isoWeekCalendar.Object,
            unitOfWork.Object);

        await Assert.ThrowsAsync<NoEligibleStudyRecordException>(
            () => handler.Handle(
                new DeleteLastStudyRecordCommand(studyAreaWeekId),
                CancellationToken.None));

        studyRecordRepository.Verify(
            repository => repository.Delete(
                It.IsAny<StudyRecord>()),
            Times.Never);

        unitOfWork.Verify(
            work => work.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_QuandoStudyAreaWeekNaoPertenceSemanaCorrente_LancaRecordNotInCurrentWeekException()
    {
        var studyAreaWeekId = Guid.NewGuid();
        var weeklyAssessmentId = Guid.NewGuid();

        var currentWeekStart = new DateOnly(2026, 10, 5);
        var previousWeekStart = currentWeekStart.AddDays(-7);

        var studyAreaWeek = new StudyAreaWeek(
            studyAreaWeekId,
            previousWeekStart,
            Guid.NewGuid(),
            Guid.NewGuid(),
            weeklyAssessmentId);

        var studyAreaWeekRepository =
            new Mock<IStudyAreaWeekRepository>();

        studyAreaWeekRepository
            .Setup(repository =>
                repository.GetByIdAsync(
                    studyAreaWeekId,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(studyAreaWeek);

        var currentWeekProvider =
            new Mock<ICurrentWeekProvider>();

        currentWeekProvider
            .Setup(provider => provider.GetCurrentWeek())
            .Returns(new WeekRange(currentWeekStart));

        var assessmentRepository =
            new Mock<IStudyAreaWeekAssessmentRepository>();

        var weeklyAssessmentRepository =
            new Mock<IWeeklyAssessmentRepository>();

        var studyRecordRepository =
            new Mock<IStudyRecordRepository>();

        var lifoSelector =
            new Mock<IStudyRecordLifoSelector>();

        var isoWeekCalendar =
            new Mock<IIsoWeekCalendar>();

        var unitOfWork =
            new Mock<IUnitOfWork>();

        var handler = new DeleteLastStudyRecordHandler(
            studyAreaWeekRepository.Object,
            assessmentRepository.Object,
            weeklyAssessmentRepository.Object,
            studyRecordRepository.Object,
            lifoSelector.Object,
            currentWeekProvider.Object,
            isoWeekCalendar.Object,
            unitOfWork.Object);

        await Assert.ThrowsAsync<RecordNotInCurrentWeekException>(
            () => handler.Handle(
                new DeleteLastStudyRecordCommand(studyAreaWeekId),
                CancellationToken.None));

        lifoSelector.Verify(
            selector => selector.SelectLastEligibleAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        studyRecordRepository.Verify(
            repository => repository.Delete(
                It.IsAny<StudyRecord>()),
            Times.Never);

        unitOfWork.Verify(
            work => work.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_QuandoWeeklyAssessmentNaoExiste_LancaDomainExceptionENaoExcluiRegistro()
    {
        var studyAreaWeekId = Guid.NewGuid();
        var weeklyAssessmentId = Guid.NewGuid();
        var weekStartDate = new DateOnly(2026, 10, 5);

        var isoWeek = System.Globalization.ISOWeek.GetWeekOfYear(
            weekStartDate.ToDateTime(TimeOnly.MinValue));

        var isoYear = System.Globalization.ISOWeek.GetYear(
            weekStartDate.ToDateTime(TimeOnly.MinValue));

        var studyAreaWeek = new StudyAreaWeek(
            studyAreaWeekId,
            weekStartDate,
            Guid.NewGuid(),
            Guid.NewGuid(),
            weeklyAssessmentId);

        var studyAreaWeekRepository =
            new Mock<IStudyAreaWeekRepository>();

        studyAreaWeekRepository
            .Setup(repository =>
                repository.GetByIdAsync(
                    studyAreaWeekId,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(studyAreaWeek);

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

        var currentWeekProvider =
            new Mock<ICurrentWeekProvider>();

        currentWeekProvider
            .Setup(provider => provider.GetCurrentWeek())
            .Returns(new WeekRange(weekStartDate));

        var isoWeekCalendar =
            new Mock<IIsoWeekCalendar>();

        isoWeekCalendar
            .Setup(calendar =>
                calendar.GetIsoWeek(weekStartDate))
            .Returns(new IsoWeek(isoYear, isoWeek));

        var weeklyAssessmentRepository =
            new Mock<IWeeklyAssessmentRepository>();

        weeklyAssessmentRepository
            .Setup(repository =>
                repository.GetByYearAndWeekNumberAsync(
                    isoYear,
                    isoWeek,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync((WeeklyAssessment?)null);

        var assessmentRepository =
            new Mock<IStudyAreaWeekAssessmentRepository>();

        var studyRecordRepository =
            new Mock<IStudyRecordRepository>();

        var lifoSelector =
            new Mock<IStudyRecordLifoSelector>();

        var unitOfWork =
            new Mock<IUnitOfWork>();

        var handler = new DeleteLastStudyRecordHandler(
            studyAreaWeekRepository.Object,
            assessmentRepository.Object,
            weeklyAssessmentRepository.Object,
            studyRecordRepository.Object,
            lifoSelector.Object,
            currentWeekProvider.Object,
            isoWeekCalendar.Object,
            unitOfWork.Object);

        await Assert.ThrowsAsync<DomainException>(
            () => handler.Handle(
                new DeleteLastStudyRecordCommand(studyAreaWeekId),
                CancellationToken.None));

        lifoSelector.Verify(
            selector => selector.SelectLastEligibleAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        studyRecordRepository.Verify(
            repository => repository.Delete(
                It.IsAny<StudyRecord>()),
            Times.Never);

        unitOfWork.Verify(
            work => work.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
}