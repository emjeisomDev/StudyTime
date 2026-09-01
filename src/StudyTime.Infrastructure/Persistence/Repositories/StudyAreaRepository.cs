using Microsoft.EntityFrameworkCore;
using StudyTime.Application.StudyAreas;
using StudyTime.Domain.Entities;

namespace StudyTime.Infrastructure.Persistence.Repositories;

public sealed class StudyAreaRepository(StudyTimeDbContext dbContext) : IStudyAreaRepository
{
    public async Task<StudyArea?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => await dbContext.StudyAreas.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<StudyArea>> ListAsync(CancellationToken cancellationToken)
        => await dbContext.StudyAreas
                            .AsNoTracking()
                            .OrderBy(x => x.Name)
                            .ToListAsync(cancellationToken);

    public async Task<bool> ExistsByNameAsync(string name, Guid? excludedId, CancellationToken cancellationToken)
        => await dbContext.StudyAreas.AnyAsync(
            x => x.Name == name && (!excludedId.HasValue || x.Id != excludedId.Value),
            cancellationToken);

    public async Task<bool> HasDependenciesAsync(Guid studyAreaId, CancellationToken cancellationToken)
        => await dbContext.StudyAreaWeeks.AnyAsync(x => x.StudyAreaId == studyAreaId, cancellationToken);

    public void Add(StudyArea studyArea)
        => dbContext.StudyAreas.Add(studyArea);

    public void Remove(StudyArea studyArea)
        => dbContext.StudyAreas.Remove(studyArea);

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => dbContext.SaveChangesAsync(cancellationToken);
}