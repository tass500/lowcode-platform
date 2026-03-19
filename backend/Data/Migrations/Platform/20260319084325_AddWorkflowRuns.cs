using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LowCodePlatform.Backend.Data.Migrations.Platform
{
    /// <inheritdoc />
    public partial class AddWorkflowRuns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "workflow_run",
                columns: table => new
                {
                    workflow_run_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    workflow_definition_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    state = table.Column<string>(type: "TEXT", nullable: false),
                    started_at_utc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    finished_at_utc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    trace_id = table.Column<string>(type: "TEXT", nullable: false),
                    error_code = table.Column<string>(type: "TEXT", nullable: true),
                    error_message = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workflow_run", x => x.workflow_run_id);
                    table.ForeignKey(
                        name: "FK_workflow_run_workflow_definition_workflow_definition_id",
                        column: x => x.workflow_definition_id,
                        principalTable: "workflow_definition",
                        principalColumn: "workflow_definition_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "workflow_step_run",
                columns: table => new
                {
                    workflow_step_run_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    workflow_run_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    step_key = table.Column<string>(type: "TEXT", nullable: false),
                    step_type = table.Column<string>(type: "TEXT", nullable: false),
                    step_config_json = table.Column<string>(type: "TEXT", nullable: true),
                    state = table.Column<string>(type: "TEXT", nullable: false),
                    attempt = table.Column<int>(type: "INTEGER", nullable: false),
                    last_error_code = table.Column<string>(type: "TEXT", nullable: true),
                    last_error_message = table.Column<string>(type: "TEXT", nullable: true),
                    started_at_utc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    finished_at_utc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workflow_step_run", x => x.workflow_step_run_id);
                    table.ForeignKey(
                        name: "FK_workflow_step_run_workflow_run_workflow_run_id",
                        column: x => x.workflow_run_id,
                        principalTable: "workflow_run",
                        principalColumn: "workflow_run_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_workflow_run_state",
                table: "workflow_run",
                column: "state");

            migrationBuilder.CreateIndex(
                name: "IX_workflow_run_workflow_definition_id",
                table: "workflow_run",
                column: "workflow_definition_id");

            migrationBuilder.CreateIndex(
                name: "IX_workflow_step_run_state",
                table: "workflow_step_run",
                column: "state");

            migrationBuilder.CreateIndex(
                name: "IX_workflow_step_run_workflow_run_id",
                table: "workflow_step_run",
                column: "workflow_run_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "workflow_step_run");

            migrationBuilder.DropTable(
                name: "workflow_run");
        }
    }
}
