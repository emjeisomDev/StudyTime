using Moq;
using StudyTime.Domain.Enums;
using StudyTime.Domain.Entities;
using StudyTime.Domain.Exceptions;
using StudyTime.Domain.ValueObjects;
using StudyTime.Domain.Abstractions;
using StudyTime.Domain.Abstractions.Repositories;
using StudyTime.Application.StudyAreaWeeks.Commands;

namespace StudyTime.Application.Tests.StudyAreaWeeks;

public sealed class UpdateStudyAreaWeekHandlerTests
{
    private static readonly DateOnly WeekStartDate =
        new(2026, 9, 28);

    private static readonly Guid StudyAreaWeekId =
        Guid.NewGuid();

    private static readonly Guid StudyAreaId =
        Guid.NewGuid();

    private static readonly Guid StudyPlanId =
        Guid.NewGuid();

    private static readonly Guid WeeklyAssessmentId =
        Guid.NewGuid();

    private readonly Mock<IStudyAreaRepository> _studyAreaRepository = new();
    private readonly Mock<IStudyPlanRepository> _studyPlanRepository = new();
    private readonly Mock<IStudyAreaWeekRepository> _studyAreaWeekRepository = new();
    private readonly Mock<IStudyAreaWeekAssessmentRepository>
        _studyAreaWeekAssessmentRepository = new();
    private readonly Mock<IStudyRecordRepository> _studyRecordRepository = new();
    private readonly Mock<IWeeklyAssessmentRepository>
        _weeklyAssessmentRepository = new();
    private readonly Mock<ICurrentWeekProvider> _currentWeekProvider = new();
    private readonly Mock<IIsoWeekCalendar> _isoWeekCalendar = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private readonly StudyAreaWeek _studyAreaWeek;
    private readonly StudyAreaWeekAssessment _individualAssessment;
    private readonly WeeklyAssessment _weeklyAssessment;
    private readonly StudyArea _studyArea;
    private readonly StudyPlan _studyPlan;

    public UpdateStudyAreaWeekHandlerTests()
    {
        _studyAreaWeek = new StudyAreaWeek(
            StudyAreaWeekId,
            WeekStartDate,
            StudyAreaId,
            StudyPlanId,
            WeeklyAssessmentId);

        _individualAssessment = new StudyAreaWeekAssessment(
            600m,
            StudyAreaWeekId,
            120);

        _weeklyAssessment = new WeeklyAssessment(
            WeeklyAssessmentId,
            40,
            2026,
            1500m,
            1500);

        _studyArea = new StudyArea(
            StudyAreaId,
            "Matemática",
            new Minutes(600));

        _studyPlan = new StudyPlan(
            StudyPlanId,
            "Plano padrão",
            new Coefficient(1m));

        ConfigureCurrentWeek();
    }

    [Fact]
    public async Task Handle_ConfiguracaoValida_AtualizaMetasEMantemRegistros()
    {
        var otherWeek = new StudyAreaWeek(
            Guid.NewGuid(),
            WeekStartDate,
            Guid.NewGuid(),
            Guid.NewGuid(),
            WeeklyAssessmentId);

        var otherAssessment = new StudyAreaWeekAssessment(
            900m,
            otherWeek.Id,
            300);

        var records = new[]
        {
            new StudyRecord(
                Guid.NewGuid(),
                WeekStartDate,
                new Minutes(120),
                StudyAreaWeekId)
        };

        ConfigureExistingConfiguration();
        _studyAreaRepository
            .Setup(repository => repository.GetByIdAsync(
                StudyAreaId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(_studyArea);

        _studyPlanRepository
            .Setup(repository => repository.GetByIdAsync(
                StudyPlanId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(_studyPlan);

        _studyAreaWeekRepository
            .Setup(repository => repository.ExistsForStudyAreaInWeekAsync(
                StudyAreaId,
                WeekStartDate,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _studyAreaWeekRepository
            .Setup(repository => repository.GetByWeekStartDateAsync(
                WeekStartDate,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { _studyAreaWeek, otherWeek });

        _studyAreaWeekAssessmentRepository
            .Setup(repository => repository.GetByStudyAreaWeekIdAsync(
                otherWeek.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(otherAssessment);

        _studyRecordRepository
            .Setup(repository => repository.GetByStudyAreaWeekIdAsync(
                StudyAreaWeekId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(records);

        _studyRecordRepository
            .Setup(repository => repository.GetByStudyAreaWeekIdAsync(
                otherWeek.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<StudyRecord>());

        var handler = CreateHandler();

        var result = await handler.Handle(
            new UpdateStudyAreaWeekCommand(
                StudyAreaWeekId,
                StudyAreaId,
                StudyPlanId),
            CancellationToken.None);

        Assert.Equal(StudyAreaWeekId, result.Id);
        Assert.Equal(WeekStartDate, result.WeekStartDate);
        Assert.Equal(StudyAreaId, result.StudyAreaId);
        Assert.Equal(StudyPlanId, result.StudyPlanId);
        Assert.Equal(WeeklyAssessmentId, result.WeeklyAssessmentId);
        Assert.Equal(600m, result.WeekIndividualGoal);
        Assert.Equal(120, result.MinutesStudied);
        Assert.False(result.GoalAchieved);

        _studyAreaWeekRepository.Verify(
            repository => repository.Update(
                It.Is<StudyAreaWeek>(week =>
                    week.Id == StudyAreaWeekId &&
                    week.StudyAreaId == StudyAreaId &&
                    week.StudyPlanId == StudyPlanId &&
                    week.WeeklyAssessmentId == WeeklyAssessmentId)),
            Times.Once);

        _studyAreaWeekAssessmentRepository.Verify(
            repository => repository.Update(
                It.Is<StudyAreaWeekAssessment>(assessment =>
                    assessment.StudyAreaWeekId == StudyAreaWeekId &&
                    assessment.WeekIndividualGoal == 600m &&
                    assessment.MinutesStudied == 120)),
            Times.Once);

        _weeklyAssessmentRepository.Verify(
            repository => repository.Update(
                It.Is<WeeklyAssessment>(assessment =>
                    assessment.Id == WeeklyAssessmentId &&
                    assessment.WeekGlobalGoal == 1500m &&
                    assessment.MinutesStudied == 420)),
            Times.Once);

        _studyRecordRepository.Verify(
            repository => repository.Delete(It.IsAny<StudyRecord>()),
            Times.Never);

        _unitOfWork.Verify(
            unitOfWork => unitOfWork.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_IdVazio_LancaArgumentException()
    {
        var handler = CreateHandler();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            handler.Handle(
                new UpdateStudyAreaWeekCommand(
                    Guid.Empty,
                    StudyAreaId,
                    StudyPlanId),
                CancellationToken.None));

        _studyAreaWeekRepository.Verify(
            repository => repository.GetByIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _unitOfWork.Verify(
            unitOfWork => unitOfWork.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ConfiguracaoInexistente_LancaKeyNotFoundException()
    {
        _studyAreaWeekRepository
            .Setup(repository => repository.GetByIdAsync(
                StudyAreaWeekId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((StudyAreaWeek?)null);

        var handler = CreateHandler();

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            handler.Handle(
                new UpdateStudyAreaWeekCommand(
                    StudyAreaWeekId,
                    StudyAreaId,
                    StudyPlanId),
                CancellationToken.None));

        _unitOfWork.Verify(
            unitOfWork => unitOfWork.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_MetaGlobalNaoAtingida_LancaWeekConfigurationLockedException()
    {
        _weeklyAssessmentRepository
            .Setup(repository => repository.GetByYearAndWeekNumberAsync(
                2026,
                40,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WeeklyAssessment(
                WeeklyAssessmentId,
                40,
                2026,
                1500m,
                1499));

        _studyAreaWeekRepository
            .Setup(repository => repository.GetByIdAsync(
                StudyAreaWeekId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(_studyAreaWeek);

        var handler = CreateHandler();

        await Assert.ThrowsAsync<WeekConfigurationLockedException>(() =>
            handler.Handle(
                new UpdateStudyAreaWeekCommand(
                    StudyAreaWeekId,
                    StudyAreaId,
                    StudyPlanId),
                CancellationToken.None));

        _studyAreaRepository.Verify(
            repository => repository.GetByIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _unitOfWork.Verify(
            unitOfWork => unitOfWork.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_PlanoInativo_LancaDomainException()
    {
        ConfigureExistingConfiguration();

        var inactivePlan = new StudyPlan(
            StudyPlanId,
            "Plano inativo",
            new Coefficient(1m));

        inactivePlan.ChangeStatus(StudyPlanStatus.Inactive);

        _studyAreaRepository
            .Setup(repository => repository.GetByIdAsync(
                StudyAreaId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(_studyArea);

        _studyPlanRepository
            .Setup(repository => repository.GetByIdAsync(
                StudyPlanId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(inactivePlan);

        var handler = CreateHandler();

        await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(
                new UpdateStudyAreaWeekCommand(
                    StudyAreaWeekId,
                    StudyAreaId,
                    StudyPlanId),
                CancellationToken.None));

        _unitOfWork.Verify(
            unitOfWork => unitOfWork.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private void ConfigureCurrentWeek()
    {
        _currentWeekProvider
            .Setup(provider => provider.GetCurrentWeek())
            .Returns(new WeekRange(WeekStartDate));

        _isoWeekCalendar
            .Setup(calendar => calendar.GetIsoWeek(WeekStartDate))
            .Returns(new IsoWeek(2026, 40));
    }

    private void ConfigureExistingConfiguration()
    {
        _studyAreaWeekRepository
            .Setup(repository => repository.GetByIdAsync(
                StudyAreaWeekId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(_studyAreaWeek);

        _weeklyAssessmentRepository
            .Setup(repository => repository.GetByYearAndWeekNumberAsync(
                2026,
                40,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(_weeklyAssessment);

        _studyAreaWeekAssessmentRepository
            .Setup(repository => repository.GetByStudyAreaWeekIdAsync(
                StudyAreaWeekId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(_individualAssessment);
    }

    private UpdateStudyAreaWeekHandler CreateHandler()
    {
        return new UpdateStudyAreaWeekHandler(
            _studyAreaRepository.Object,
            _studyPlanRepository.Object,
            _studyAreaWeekRepository.Object,
            _studyAreaWeekAssessmentRepository.Object,
            _studyRecordRepository.Object,
            _weeklyAssessmentRepository.Object,
            _currentWeekProvider.Object,
            _isoWeekCalendar.Object,
            _unitOfWork.Object);
    }
}