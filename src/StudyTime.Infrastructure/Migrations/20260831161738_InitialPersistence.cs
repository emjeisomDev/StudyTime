using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudyTime.Infrastructure.Migrations;

public partial class InitialPersistence : Migration
{
    private static readonly string[] StudyAreaWeekAreaDateIndexColumns = ["study_area_id", "week_start_date"];
    private static readonly string[] WeeklyAssessmentYearWeekIndexColumns = ["year", "week_number"];

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "tb_study_area",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "varchar(80)", nullable: false),
                std_week_study_time = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_tb_study_area", x => x.id);
                table.CheckConstraint("ck_tb_study_area_std_week_study_time_positive", "std_week_study_time > 0");
            });

        migrationBuilder.CreateTable(
            name: "tb_study_plan",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "varchar(80)", nullable: false),
                coefficient = table.Column<decimal>(type: "numeric(3,2)", nullable: false),
                status = table.Column<string>(type: "varchar(20)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_tb_study_plan", x => x.id);
                table.CheckConstraint("ck_tb_study_plan_coefficient_positive", "coefficient > 0");
                table.CheckConstraint("ck_tb_study_plan_status", "status IN ('active','inactive')");
            });

        migrationBuilder.CreateTable(
            name: "tb_weekly_assessment",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                week_number = table.Column<int>(type: "integer", nullable: false),
                year = table.Column<int>(type: "integer", nullable: false),
                week_global_goal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                minutes_studied = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_tb_weekly_assessment", x => x.id);
                table.CheckConstraint("ck_tb_weekly_assessment_global_goal_positive", "week_global_goal > 0");
                table.CheckConstraint("ck_tb_weekly_assessment_minutes_studied_non_negative", "minutes_studied >= 0");
                table.CheckConstraint("ck_tb_weekly_assessment_week_number", "week_number BETWEEN 1 AND 53");
                table.CheckConstraint("ck_tb_weekly_assessment_year_positive", "year > 0");
            });

        migrationBuilder.CreateTable(
            name: "tb_study_area_week",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                week_start_date = table.Column<DateOnly>(type: "date", nullable: false),
                study_area_id = table.Column<Guid>(type: "uuid", nullable: false),
                study_plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                weekly_assessment_id = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_tb_study_area_week", x => x.id);
                table.CheckConstraint("ck_tb_study_area_week_monday", "EXTRACT(ISODOW FROM week_start_date) = 1");
                table.ForeignKey(
                    name: "fk_study_area_week_study_area",
                    column: x => x.study_area_id,
                    principalTable: "tb_study_area",
                    principalColumn: "id");
                table.ForeignKey(
                    name: "fk_study_area_week_study_plan",
                    column: x => x.study_plan_id,
                    principalTable: "tb_study_plan",
                    principalColumn: "id");
                table.ForeignKey(
                    name: "fk_study_area_week_weekly_assessment",
                    column: x => x.weekly_assessment_id,
                    principalTable: "tb_weekly_assessment",
                    principalColumn: "id");
            });

        migrationBuilder.CreateTable(
            name: "tb_study_area_week_assessment",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                week_individual_goal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                minutes_studied = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                study_area_week_id = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_tb_study_area_week_assessment", x => x.id);
                table.CheckConstraint("ck_tb_study_area_week_assessment_goal_positive", "week_individual_goal > 0");
                table.CheckConstraint("ck_tb_study_area_week_assessment_minutes_non_negative", "minutes_studied >= 0");
                table.ForeignKey(
                    name: "fk_study_area_week_assessment",
                    column: x => x.study_area_week_id,
                    principalTable: "tb_study_area_week",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "tb_study_record",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                date = table.Column<DateOnly>(
                    type: "date",
                    nullable: false,
                    defaultValueSql: "((NOW() AT TIME ZONE 'America/Sao_Paulo')::date)"),
                created_at = table.Column<DateTimeOffset>(
                    type: "timestamptz",
                    nullable: false,
                    defaultValueSql: "CURRENT_TIMESTAMP"),
                minutes = table.Column<int>(type: "integer", nullable: false),
                study_area_week_id = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_tb_study_record", x => x.id);
                table.CheckConstraint("ck_tb_study_record_minutes_positive", "minutes > 0");
                table.ForeignKey(
                    name: "fk_study_record_study_area_week",
                    column: x => x.study_area_week_id,
                    principalTable: "tb_study_area_week",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "ux_tb_study_area_name",
            table: "tb_study_area",
            column: "name",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_tb_study_area_week_study_plan_id",
            table: "tb_study_area_week",
            column: "study_plan_id");

        migrationBuilder.CreateIndex(
            name: "IX_tb_study_area_week_weekly_assessment_id",
            table: "tb_study_area_week",
            column: "weekly_assessment_id");

        migrationBuilder.CreateIndex(
            name: "ux_tb_study_area_week_area_date",
            table: "tb_study_area_week",
            columns: StudyAreaWeekAreaDateIndexColumns,
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ux_tb_study_area_week_assessment_week",
            table: "tb_study_area_week_assessment",
            column: "study_area_week_id",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_tb_study_record_study_area_week_id",
            table: "tb_study_record",
            column: "study_area_week_id");

        migrationBuilder.CreateIndex(
            name: "ux_tb_weekly_assessment_year_week_number",
            table: "tb_weekly_assessment",
            columns: WeeklyAssessmentYearWeekIndexColumns,
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "tb_study_area_week_assessment");

        migrationBuilder.DropTable(
            name: "tb_study_record");

        migrationBuilder.DropTable(
            name: "tb_study_area_week");

        migrationBuilder.DropTable(
            name: "tb_study_area");

        migrationBuilder.DropTable(
            name: "tb_study_plan");

        migrationBuilder.DropTable(
            name: "tb_weekly_assessment");
    }
}