using Microsoft.EntityFrameworkCore;
using StudyTime.Infrastructure.Persistence;
using StudyTime.Infrastructure.Tests.Fixtures;

namespace StudyTime.Infrastructure.Tests.Persistence;

public sealed class MigrationTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _fixture;

    public MigrationTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Migration_schemaApplied_containsExpectedTablesAndColumns()
    {
        await using StudyTimeDbContext context = _fixture.CreateDbContext();

        IReadOnlyList<string> tables =
            await context.Database.SqlQueryRaw<string>(
                """
                SELECT table_name
                FROM information_schema.tables
                WHERE table_schema = 'public'
                  AND table_name LIKE 'tb_%'
                ORDER BY table_name;
                """)
            .ToListAsync();

        Assert.Equal(
            [
                "tb_study_area",
                "tb_study_area_week",
                "tb_study_area_week_assessment",
                "tb_study_plan",
                "tb_study_record",
                "tb_weekly_assessment"
            ],
            tables);

        IReadOnlyList<string> studyRecordColumns =
            await context.Database.SqlQueryRaw<string>(
                """
                SELECT column_name
                FROM information_schema.columns
                WHERE table_schema = 'public'
                  AND table_name = 'tb_study_record'
                ORDER BY ordinal_position;
                """)
            .ToListAsync();

        Assert.Equal(
            [
                "id",
                "date",
                "created_at",
                "minutes",
                "study_area_week_id"
            ],
            studyRecordColumns);

        IReadOnlyList<string> weeklyAssessmentColumns =
            await context.Database.SqlQueryRaw<string>(
                """
                SELECT column_name
                FROM information_schema.columns
                WHERE table_schema = 'public'
                  AND table_name = 'tb_weekly_assessment'
                ORDER BY ordinal_position;
                """)
            .ToListAsync();

        Assert.Equal(
            [
                "id",
                "week_number",
                "year",
                "week_global_goal",
                "minutes_studied"
            ],
            weeklyAssessmentColumns);
    }

    [Fact]
    public async Task Migration_schemaApplied_containsExpectedCheckConstraints()
    {
        await using StudyTimeDbContext context = _fixture.CreateDbContext();

        IReadOnlyList<string> checkConstraints =
            await context.Database.SqlQueryRaw<string>(
                """
                SELECT conname
                FROM pg_constraint
                WHERE conrelid IN
                (
                    'tb_study_area'::regclass,
                    'tb_study_area_week'::regclass,
                    'tb_study_area_week_assessment'::regclass,
                    'tb_study_plan'::regclass,
                    'tb_study_record'::regclass,
                    'tb_weekly_assessment'::regclass
                )
                AND contype = 'c'
                ORDER BY conname;
                """)
            .ToListAsync();

        Assert.Equal(
            [
                "ck_tb_study_area_std_week_study_time_positive",
                "ck_tb_study_area_week_assessment_individual_goal_positive",
                "ck_tb_study_area_week_assessment_minutes_studied_non_negative",
                "ck_tb_study_area_week_start_date_monday",
                "ck_tb_study_plan_coefficient_positive",
                "ck_tb_study_plan_status_valid",
                "ck_tb_study_record_minutes_positive",
                "ck_tb_weekly_assessment_global_goal_positive",
                "ck_tb_weekly_assessment_minutes_studied_non_negative",
                "ck_tb_weekly_assessment_week_number_valid",
                "ck_tb_weekly_assessment_year_positive"
            ],
            checkConstraints);
    }

    [Fact]
    public async Task Migration_schemaApplied_containsExpectedUniqueIndexes()
    {
        await using StudyTimeDbContext context = _fixture.CreateDbContext();

        IReadOnlyList<UniqueIndexInfo> indexes =
            await context.Database.SqlQueryRaw<UniqueIndexInfo>(
            """
                SELECT
                    indexname AS "Name",
                    indexdef LIKE '%UNIQUE INDEX%' AS "IsUnique"
                FROM pg_indexes
                WHERE schemaname = 'public'
                AND tablename IN
                (
                    'tb_study_area',
                    'tb_study_area_week',
                    'tb_study_area_week_assessment',
                    'tb_study_plan',
                    'tb_study_record',
                    'tb_weekly_assessment'
                )
                AND indexdef LIKE '%UNIQUE%'
                ORDER BY indexname;
            """)
            .ToListAsync();

        Assert.Contains(
            indexes,
            index =>
                index.Name ==
                "IX_tb_study_area_week_study_area_id_week_start_date" &&
                index.IsUnique);

        Assert.Contains(
            indexes,
            index =>
                index.Name ==
                "IX_tb_study_area_week_assessment_study_area_week_id" &&
                index.IsUnique);

        Assert.Contains(
            indexes,
            index =>
                index.Name ==
                "IX_tb_study_plan_name" &&
                index.IsUnique);

        Assert.Contains(
            indexes,
            index =>
                index.Name ==
                "IX_tb_study_area_name" &&
                index.IsUnique);

        Assert.Contains(
            indexes,
            index =>
                index.Name ==
                "IX_tb_weekly_assessment_year_week_number" &&
                index.IsUnique);
    }

    [Fact]
    public async Task Migration_schemaApplied_containsExpectedForeignKeys()
    {
        await using StudyTimeDbContext context = _fixture.CreateDbContext();

        IReadOnlyList<ForeignKeyInfo> foreignKeys =
            await context.Database.SqlQueryRaw<ForeignKeyInfo>(
                """
                SELECT
                    con.conname AS "Name",
                    child.relname AS "ChildTable",
                    parent.relname AS "ParentTable",
                    childColumn.attname AS "ChildColumn",
                    parentColumn.attname AS "ParentColumn",
                    CASE con.confdeltype
                        WHEN 'a' THEN 'NO ACTION'
                        WHEN 'c' THEN 'CASCADE'
                        WHEN 'r' THEN 'RESTRICT'
                        WHEN 'n' THEN 'SET NULL'
                        WHEN 'd' THEN 'SET DEFAULT'
                        ELSE 'UNKNOWN'
                    END AS "DeleteAction"
                FROM pg_constraint con
                INNER JOIN pg_class child
                    ON child.oid = con.conrelid
                INNER JOIN pg_class parent
                    ON parent.oid = con.confrelid
                INNER JOIN LATERAL
                (
                    SELECT attname
                    FROM pg_attribute
                    WHERE attrelid = con.conrelid
                      AND attnum = con.conkey[1]
                ) childColumn
                    ON TRUE
                INNER JOIN LATERAL
                (
                    SELECT attname
                    FROM pg_attribute
                    WHERE attrelid = con.confrelid
                      AND attnum = con.confkey[1]
                ) parentColumn
                    ON TRUE
                WHERE con.contype = 'f'
                  AND child.relnamespace = 'public'::regnamespace
                ORDER BY con.conname;
                """)
            .ToListAsync();

        Assert.Contains(
            foreignKeys,
            foreignKey =>
                foreignKey.ChildTable == "tb_study_area_week" &&
                foreignKey.ParentTable == "tb_study_area" &&
                foreignKey.ChildColumn == "study_area_id" &&
                foreignKey.ParentColumn == "id" &&
                foreignKey.DeleteAction == "NO ACTION");

        Assert.Contains(
            foreignKeys,
            foreignKey =>
                foreignKey.ChildTable == "tb_study_area_week" &&
                foreignKey.ParentTable == "tb_study_plan" &&
                foreignKey.ChildColumn == "study_plan_id" &&
                foreignKey.ParentColumn == "id" &&
                foreignKey.DeleteAction == "NO ACTION");

        Assert.Contains(
            foreignKeys,
            foreignKey =>
                foreignKey.ChildTable == "tb_study_area_week" &&
                foreignKey.ParentTable == "tb_weekly_assessment" &&
                foreignKey.ChildColumn == "weekly_assessment_id" &&
                foreignKey.ParentColumn == "id" &&
                foreignKey.DeleteAction == "NO ACTION");

        Assert.Contains(
            foreignKeys,
            foreignKey =>
                foreignKey.ChildTable == "tb_study_area_week_assessment" &&
                foreignKey.ParentTable == "tb_study_area_week" &&
                foreignKey.ChildColumn == "study_area_week_id" &&
                foreignKey.ParentColumn == "id" &&
                foreignKey.DeleteAction == "CASCADE");

        Assert.Contains(
            foreignKeys,
            foreignKey =>
                foreignKey.ChildTable == "tb_study_record" &&
                foreignKey.ParentTable == "tb_study_area_week" &&
                foreignKey.ChildColumn == "study_area_week_id" &&
                foreignKey.ParentColumn == "id" &&
                foreignKey.DeleteAction == "CASCADE");
    }

    [Fact]
    public async Task Migration_schemaApplied_containsExpectedStudyRecordDefaults()
    {
        await using StudyTimeDbContext context = _fixture.CreateDbContext();

        StudyRecordColumnInfo? dateColumn =
            await context.Database.SqlQueryRaw<StudyRecordColumnInfo>(
                """
                SELECT
                    column_name AS "ColumnName",
                    column_default AS "ColumnDefault"
                FROM information_schema.columns
                WHERE table_schema = 'public'
                  AND table_name = 'tb_study_record'
                  AND column_name = 'date'
                """)
            .SingleOrDefaultAsync();

        StudyRecordColumnInfo? createdAtColumn =
            await context.Database.SqlQueryRaw<StudyRecordColumnInfo>(
                """
                SELECT
                    column_name AS "ColumnName",
                    column_default AS "ColumnDefault"
                FROM information_schema.columns
                WHERE table_schema = 'public'
                  AND table_name = 'tb_study_record'
                  AND column_name = 'created_at'
                """)
            .SingleOrDefaultAsync();

        Assert.NotNull(dateColumn);
        Assert.Contains(
            "America/Sao_Paulo",
            dateColumn!.ColumnDefault,
            StringComparison.Ordinal);

        Assert.Contains(
            "NOW()",
            dateColumn.ColumnDefault,
            StringComparison.OrdinalIgnoreCase);

        Assert.NotNull(createdAtColumn);
        Assert.Contains(
            "CURRENT_TIMESTAMP",
            createdAtColumn!.ColumnDefault,
            StringComparison.OrdinalIgnoreCase);
    }

    private sealed class UniqueIndexInfo
    {
        public string Name { get; init; } = string.Empty;
        public bool IsUnique { get; init; }
    }

    private sealed class ForeignKeyInfo
    {
        public string Name { get; init; } = string.Empty;
        public string ChildTable { get; init; } = string.Empty;
        public string ParentTable { get; init; } = string.Empty;
        public string ChildColumn { get; init; } = string.Empty;
        public string ParentColumn { get; init; } = string.Empty;
        public string DeleteAction { get; init; } = string.Empty;
    }

    private sealed class StudyRecordColumnInfo
    {
        public string ColumnName { get; init; } = string.Empty;
        public string ColumnDefault { get; init; } = string.Empty;
    }
}