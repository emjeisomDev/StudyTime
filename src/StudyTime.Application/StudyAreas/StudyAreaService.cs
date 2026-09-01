using StudyTime.Domain.Entities;

namespace StudyTime.Application.StudyAreas;

public sealed class StudyAreaService(IStudyAreaRepository repository) : IStudyAreaService
{
    public async Task<StudyAreaResponse> CreateAsync(CreateStudyAreaRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var studyArea = StudyArea.Create(request.Name ?? string.Empty, request.StdWeekStudyTime);

        if (await repository.ExistsByNameAsync(studyArea.Name, null, cancellationToken))
            throw new InvalidOperationException($"A study area with name '{studyArea.Name}' already exists.");

        repository.Add(studyArea);
        await repository.SaveChangesAsync(cancellationToken);

        return Map(studyArea);
    }

    public async Task<IReadOnlyList<StudyAreaResponse>> ListAsync(CancellationToken cancellationToken)
    {
        var studyAreas = await repository.ListAsync(cancellationToken);
        return studyAreas.Select(Map).ToArray();
    }

    public async Task<StudyAreaResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var studyArea = await repository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Study area '{id}' was not found.");

        return Map(studyArea);
    }

    public async Task<StudyAreaResponse> UpdateAsync(Guid id, UpdateStudyAreaRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var studyArea = await repository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Study area '{id}' was not found.");

        studyArea.Rename(request.Name ?? string.Empty);

        if (await repository.ExistsByNameAsync(studyArea.Name, id, cancellationToken))
            throw new InvalidOperationException($"A study area with name '{studyArea.Name}' already exists.");

        studyArea.ChangeStandardWeeklyStudyTime(request.StdWeekStudyTime);
        await repository.SaveChangesAsync(cancellationToken);

        return Map(studyArea);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var studyArea = await repository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Study area '{id}' was not found.");

        if (await repository.HasDependenciesAsync(id, cancellationToken))
            throw new InvalidOperationException($"Study area '{studyArea.Name}' cannot be deleted because it is referenced by a weekly configuration.");

        repository.Remove(studyArea);
        await repository.SaveChangesAsync(cancellationToken);
    }

    private static StudyAreaResponse Map(StudyArea studyArea)
        => new(studyArea.Id, studyArea.Name, studyArea.StdWeekStudyTime);
}