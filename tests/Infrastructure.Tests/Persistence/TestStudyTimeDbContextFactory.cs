using Microsoft.EntityFrameworkCore;
using StudyTime.Infrastructure.Persistence;

namespace Infrastructure.Tests.Persistence;

internal static class TestStudyTimeDbContextFactory
{
    public static StudyTimeDbContext Create(string connectionString)
    {
        DbContextOptions<StudyTimeDbContext> options =
            new DbContextOptionsBuilder<StudyTimeDbContext>()
                .UseNpgsql(connectionString)
                .EnableDetailedErrors()
                .Options;

        return new StudyTimeDbContext(options);
    }
}