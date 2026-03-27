using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LowCodePlatform.Backend.Data.Migrations.Platform
{
    /// <inheritdoc />
    public partial class AddWorkflowSchedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "schedule_cron",
                table: "workflow_definition",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "schedule_enabled",
                table: "workflow_definition",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "schedule_next_due_utc",
                table: "workflow_definition",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_workflow_definition_schedule_enabled_schedule_next_due_utc",
                table: "workflow_definition",
                columns: new[] { "schedule_enabled", "schedule_next_due_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_workflow_definition_schedule_enabled_schedule_next_due_utc",
                table: "workflow_definition");

            migrationBuilder.DropColumn(
                name: "schedule_cron",
                table: "workflow_definition");

            migrationBuilder.DropColumn(
                name: "schedule_enabled",
                table: "workflow_definition");

            migrationBuilder.DropColumn(
                name: "schedule_next_due_utc",
                table: "workflow_definition");
        }
    }
}
