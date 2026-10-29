using Moq;
using Xunit;
using StudyTime.Domain.Enums;
using StudyTime.Domain.Entities;
using StudyTime.Application.Dtos;
using StudyTime.Domain.Exceptions;
using StudyTime.Application.StudyAreaWeeks.Commands;

namespace StudyTime.Application.Tests.Handlers
{

    public sealed class BootstrapSystemHandlerTests
    {
        [Fact]
        public async Task Handle_ConfiguracaoValida_CriaAvaliacaoSemanalEConfiguracoesNaOrdemCorreta()
        {
            // Arrange
            WeeklyConfigurationHandlerTestFixture fixture = new();
            fixture.WeeklyAssessmentsByIsoWeek.Clear();
            StudyArea mathematics = WeeklyConfigurationHandlerTestFixture.CreateStudyArea("Matemática", 900);
            StudyArea physics = WeeklyConfigurationHandlerTestFixture.CreateStudyArea("Física", 600);
            fixture.AddStudyArea(mathematics);
            fixture.AddStudyArea(physics);
            StudyPlan plan = fixture.TargetStudyPlan;
            BootstrapSystemCommand command = new(
                new[]
                {
                    new StudyAreaWeekBatchItemDto(mathematics.Id, plan.Id),
                    new StudyAreaWeekBatchItemDto(physics.Id, plan.Id)
                });
            BootstrapSystemHandler handler = CreateHandler(fixture);
            using CancellationTokenSource cancellationTokenSource = new();
            CancellationToken cancellationToken = cancellationTokenSource.Token;

            // Act
            IReadOnlyList<StudyAreaWeekDto> result = await handler.Handle(command, cancellationToken);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Contains(result, item => item.StudyAreaId == mathematics.Id && item.WeekIndividualGoal == 900m);
            Assert.Contains(result, item => item.StudyAreaId == physics.Id && item.WeekIndividualGoal == 600m);
            Assert.All(result, item => Assert.Equal(WeeklyConfigurationHandlerTestFixture.CurrentWeekStart, item.WeekStartDate));
            Assert.All(result, item => Assert.False(item.GoalAchieved));

            WeeklyAssessment createdWeeklyAssessment = Assert.Single(fixture.AddedWeeklyAssessments);
            var expectedIsoWeek = WeeklyConfigurationHandlerTestFixture.GetIsoWeek(
                WeeklyConfigurationHandlerTestFixture.CurrentWeekStart);
            Assert.Equal(expectedIsoWeek.Year, createdWeeklyAssessment.Year);
            Assert.Equal(expectedIsoWeek.WeekNumber, createdWeeklyAssessment.WeekNumber);
            Assert.Equal(1500m, createdWeeklyAssessment.WeekGlobalGoal);
            Assert.Equal(
                new[]
                {
                    "WeeklyAssessment.Add",
                    "StudyAreaWeek.Add",
                    "StudyAreaWeekAssessment.Add",
                    "StudyAreaWeek.Add",
                    "StudyAreaWeekAssessment.Add",
                    "UnitOfWork.SaveChanges"
                },
                fixture.CallOrder);
            Assert.Equal(2, fixture.AddedStudyAreaWeeks.Count);
            Assert.Equal(2, fixture.AddedStudyAreaWeekAssessments.Count);
            Assert.All(fixture.AddedStudyAreaWeeks, item =>
                Assert.Equal(WeeklyConfigurationHandlerTestFixture.CurrentWeekStart, item.WeekStartDate));

            fixture.CurrentWeekProvider.Verify(item => item.GetCurrentWeek(), Times.Once);
            fixture.StudyAreaWeekRepository.Verify(item => item.GetAllAsync(cancellationToken), Times.Once);
            fixture.UnitOfWork.Verify(item => item.SaveChangesAsync(cancellationToken), Times.Once);
        }

        [Theory]
        [InlineData(1499, false)]
        [InlineData(1500, true)]
        public async Task Handle_MetaGlobalNoLimiteMinimo_AplicaRegraDe1500Minutos(int globalGoal, bool succeeds)
        {
            // Arrange
            WeeklyConfigurationHandlerTestFixture fixture = new();
            fixture.WeeklyAssessmentsByIsoWeek.Clear();
            StudyArea area = WeeklyConfigurationHandlerTestFixture.CreateStudyArea("Área limite", globalGoal);
            fixture.AddStudyArea(area);
            BootstrapSystemCommand command = new(
                new[] { new StudyAreaWeekBatchItemDto(area.Id, fixture.TargetStudyPlan.Id) });
            BootstrapSystemHandler handler = CreateHandler(fixture);

            // Act
            if (!succeeds)
            {
                WeeklyGoalNotMetException exception = await Assert.ThrowsAsync<WeeklyGoalNotMetException>(
                    () => handler.Handle(command, CancellationToken.None));

                // Assert
                Assert.Contains("at least 1500 minutes", exception.Message);
                Assert.Empty(fixture.AddedWeeklyAssessments);
                Assert.Empty(fixture.AddedStudyAreaWeeks);
                fixture.UnitOfWork.Verify(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
                return;
            }

            IReadOnlyList<StudyAreaWeekDto> result = await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.Single(result);
            Assert.Equal(1500m, Assert.Single(fixture.AddedWeeklyAssessments).WeekGlobalGoal);
            Assert.Equal(WeeklyConfigurationHandlerTestFixture.CurrentWeekStart, result[0].WeekStartDate);
            fixture.UnitOfWork.Verify(item => item.SaveChangesAsync(CancellationToken.None), Times.Once);
        }

        [Fact]
        public async Task Handle_SistemaJaInicializado_LancaDuplicateStudyAreaWeekExceptionSemPersistir()
        {
            // Arrange
            WeeklyConfigurationHandlerTestFixture fixture = new();
            fixture.InitialStudyAreaWeeks = new[] { fixture.CurrentStudyAreaWeek };
            BootstrapSystemHandler handler = CreateHandler(fixture);
            BootstrapSystemCommand command = new(
                new[] { new StudyAreaWeekBatchItemDto(fixture.TargetStudyArea.Id, fixture.TargetStudyPlan.Id) });

            // Act
            DuplicateStudyAreaWeekException exception = await Assert.ThrowsAsync<DuplicateStudyAreaWeekException>(
                () => handler.Handle(command, CancellationToken.None));

            // Assert
            Assert.Contains("already been initialized", exception.Message);
            Assert.Empty(fixture.CallOrder);
            fixture.StudyAreaRepository.Verify(
                item => item.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
            fixture.UnitOfWork.Verify(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_AreaRepetidaNaSolicitacao_LancaDuplicateStudyAreaWeekException()
        {
            // Arrange
            WeeklyConfigurationHandlerTestFixture fixture = new();
            fixture.WeeklyAssessmentsByIsoWeek.Clear();
            BootstrapSystemHandler handler = CreateHandler(fixture);
            BootstrapSystemCommand command = new(
                new[]
                {
                    new StudyAreaWeekBatchItemDto(fixture.TargetStudyArea.Id, fixture.TargetStudyPlan.Id),
                    new StudyAreaWeekBatchItemDto(fixture.TargetStudyArea.Id, fixture.TargetStudyPlan.Id)
                });

            // Act
            DuplicateStudyAreaWeekException exception = await Assert.ThrowsAsync<DuplicateStudyAreaWeekException>(
                () => handler.Handle(command, CancellationToken.None));

            // Assert
            Assert.Contains("occurs more than once", exception.Message);
            fixture.StudyAreaRepository.Verify(
                item => item.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
            fixture.UnitOfWork.Verify(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_AreaInexistente_LancaKeyNotFoundException()
        {
            // Arrange
            WeeklyConfigurationHandlerTestFixture fixture = new();
            fixture.WeeklyAssessmentsByIsoWeek.Clear();
            Guid unknownAreaId = Guid.NewGuid();
            BootstrapSystemHandler handler = CreateHandler(fixture);
            BootstrapSystemCommand command = new(
                new[] { new StudyAreaWeekBatchItemDto(unknownAreaId, fixture.TargetStudyPlan.Id) });

            // Act
            KeyNotFoundException exception = await Assert.ThrowsAsync<KeyNotFoundException>(
                () => handler.Handle(command, CancellationToken.None));

            // Assert
            Assert.Contains(unknownAreaId.ToString(), exception.Message);
            Assert.Empty(fixture.CallOrder);
            fixture.UnitOfWork.Verify(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_PlanoInexistente_LancaKeyNotFoundException()
        {
            // Arrange
            WeeklyConfigurationHandlerTestFixture fixture = new();
            fixture.WeeklyAssessmentsByIsoWeek.Clear();
            Guid unknownPlanId = Guid.NewGuid();
            BootstrapSystemHandler handler = CreateHandler(fixture);
            BootstrapSystemCommand command = new(
                new[] { new StudyAreaWeekBatchItemDto(fixture.TargetStudyArea.Id, unknownPlanId) });

            // Act
            KeyNotFoundException exception = await Assert.ThrowsAsync<KeyNotFoundException>(
                () => handler.Handle(command, CancellationToken.None));

            // Assert
            Assert.Contains(unknownPlanId.ToString(), exception.Message);
            Assert.Empty(fixture.CallOrder);
            fixture.UnitOfWork.Verify(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_PlanoInativo_LancaDomainException()
        {
            // Arrange
            WeeklyConfigurationHandlerTestFixture fixture = new();
            fixture.WeeklyAssessmentsByIsoWeek.Clear();
            fixture.TargetStudyPlan.ChangeStatus(StudyPlanStatus.Inactive);
            BootstrapSystemHandler handler = CreateHandler(fixture);
            BootstrapSystemCommand command = new(
                new[] { new StudyAreaWeekBatchItemDto(fixture.TargetStudyArea.Id, fixture.TargetStudyPlan.Id) });

            // Act
            DomainException exception = await Assert.ThrowsAsync<DomainException>(
                () => handler.Handle(command, CancellationToken.None));

            // Assert
            Assert.Contains("is inactive", exception.Message);
            Assert.Empty(fixture.CallOrder);
            fixture.UnitOfWork.Verify(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_AvaliacaoIsoJaExisteSemConfiguracoes_LancaDomainException()
        {
            // Arrange
            WeeklyConfigurationHandlerTestFixture fixture = new();
            fixture.WeeklyAssessmentsByIsoWeek.Clear();
            fixture.SetWeeklyAssessment(
                WeeklyConfigurationHandlerTestFixture.CurrentWeekStart,
                WeeklyConfigurationHandlerTestFixture.CreateWeeklyAssessment(
                    WeeklyConfigurationHandlerTestFixture.CurrentWeekStart,
                    1500m));
            BootstrapSystemHandler handler = CreateHandler(fixture);
            BootstrapSystemCommand command = new(
                new[] { new StudyAreaWeekBatchItemDto(fixture.TargetStudyArea.Id, fixture.TargetStudyPlan.Id) });

            // Act
            DomainException exception = await Assert.ThrowsAsync<DomainException>(
                () => handler.Handle(command, CancellationToken.None));

            // Assert
            Assert.Contains("weekly assessment already exists", exception.Message);
            Assert.Empty(fixture.CallOrder);
            fixture.UnitOfWork.Verify(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ListaVazia_LancaArgumentException()
        {
            // Arrange
            WeeklyConfigurationHandlerTestFixture fixture = new();
            BootstrapSystemHandler handler = CreateHandler(fixture);
            BootstrapSystemCommand command = new(Array.Empty<StudyAreaWeekBatchItemDto>());

            // Act
            ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(
                () => handler.Handle(command, CancellationToken.None));

            // Assert
            Assert.Contains("At least one item is required", exception.Message);
            fixture.StudyAreaWeekRepository.Verify(
                item => item.GetAllAsync(It.IsAny<CancellationToken>()), Times.Never);
            fixture.UnitOfWork.Verify(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        private static BootstrapSystemHandler CreateHandler(WeeklyConfigurationHandlerTestFixture fixture)
        {
            return new BootstrapSystemHandler(
                fixture.StudyAreaRepository.Object,
                fixture.StudyPlanRepository.Object,
                fixture.StudyAreaWeekRepository.Object,
                fixture.StudyAreaWeekAssessmentRepository.Object,
                fixture.WeeklyAssessmentRepository.Object,
                fixture.CurrentWeekProvider.Object,
                fixture.IsoWeekCalendar.Object,
                fixture.UnitOfWork.Object);
        }
    }

}