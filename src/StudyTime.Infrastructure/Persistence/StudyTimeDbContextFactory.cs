using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace StudyTime.Infrastructure.Persistence;

public class StudyTimeDbContextFactory : IDesignTimeDbContextFactory<StudyTimeDbContext>
{
    public StudyTimeDbContext CreateDbContext(string[] args)
    {
        string connectionString = Environment
            .GetEnvironmentVariable("STUDYTIME_CONNECTION_STRING")
            ?? "Host=localhost;Port=5432;Database=studytime;Username=studytime;Password=studytime_dev_password";

        DbContextOptionsBuilder<StudyTimeDbContext> optionsBuilder = new();

        optionsBuilder.UseNpgsql(
            connectionString,
            npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly(typeof(StudyTimeDbContext).Assembly.FullName);
            });

        return new StudyTimeDbContext(optionsBuilder.Options);
    }
}
