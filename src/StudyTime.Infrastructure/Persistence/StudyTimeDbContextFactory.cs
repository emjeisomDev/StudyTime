using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace StudyTime.Infrastructure.Persistence;

public sealed class StudyTimeDbContextFactory : IDesignTimeDbContextFactory<StudyTimeDbContext>
{
    public StudyTimeDbContext CreateDbContext(string[] args)
    {
        var connectionString = args.FirstOrDefault
            (value => value.StartsWith("Host=", StringComparison.OrdinalIgnoreCase))
            ?? "Host=localhost;Port=5432;Database=studytime;Username=studytime;Password=studytime";

        var options = new DbContextOptionsBuilder<StudyTimeDbContext>()
            .UseNpgsql(
                connectionString, npgsql => npgsql
                                            .MigrationsAssembly(typeof(StudyTimeDbContext)
                                            .Assembly
                                            .FullName)
            )
            .Options;

        return new StudyTimeDbContext(options);
    }
}
