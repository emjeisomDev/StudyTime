using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace StudyTime.Infrastructure.Persistence;

public sealed class StudyTimeDbContextFactory : IDesignTimeDbContextFactory<StudyTimeDbContext>
{
    public StudyTimeDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<StudyTimeDbContext>();
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = @"Host=localhost;Port=5432;Database=studytime;Username=studytime;Password=studytime_dev";
        }

        optionsBuilder.UseNpgsql(connectionString);
        return new StudyTimeDbContext(optionsBuilder.Options);
    }
}