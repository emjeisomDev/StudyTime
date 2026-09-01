using Microsoft.EntityFrameworkCore;
using StudyTime.Application.StudyPlans;
using StudyTime.Domain.Entities;

namespace StudyTime.Infrastructure.Persistence.Repositories;

public sealed class StudyPlanRepository(StudyTimeDbContext context) : IStudyPlanRepository
{
    public Task<StudyPlan?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => context.StudyPlans.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<StudyPlan>> ListAsync(CancellationToken cancellationToken)
        => await context.StudyPlans.AsNoTracking().OrderBy(x => x.Name).ThenBy(x => x.Id).ToArrayAsync(cancellationToken);

    public void Add(StudyPlan studyPlan)
        => context.StudyPlans.Add(studyPlan);

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => context.SaveChangesAsync(cancellationToken);
}