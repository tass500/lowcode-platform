using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LowCodePlatform.Backend.Data.Migrations.Platform
{
    /// <inheritdoc />
    public partial class InitialPlatform : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "audit_log",
                columns: table => new
                {
                    audit_log_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    actor = table.Column<string>(type: "TEXT", nullable: false),
                    action = table.Column<string>(type: "TEXT", nullable: false),
                    target = table.Column<string>(type: "TEXT", nullable: false),
                    installation_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    tenant_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    timestamp_utc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    trace_id = table.Column<string>(type: "TEXT", nullable: false),
                    details_json = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_log", x => x.audit_log_id);
                });

            migrationBuilder.CreateTable(
                name: "installation",
                columns: table => new
                {
                    installation_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    release_channel = table.Column<string>(type: "TEXT", nullable: false),
                    current_version = table.Column<string>(type: "TEXT", nullable: false),
                    supported_version = table.Column<string>(type: "TEXT", nullable: false),
                    release_date_utc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    upgrade_window_days = table.Column<int>(type: "INTEGER", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_installation", x => x.installation_id);
                });

            migrationBuilder.CreateTable(
                name: "upgrade_run",
                columns: table => new
                {
                    upgrade_run_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    installation_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    target_version = table.Column<string>(type: "TEXT", nullable: false),
                    state = table.Column<string>(type: "TEXT", nullable: false),
                    started_at_utc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    finished_at_utc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    error_code = table.Column<string>(type: "TEXT", nullable: true),
                    error_message = table.Column<string>(type: "TEXT", nullable: true),
                    trace_id = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_upgrade_run", x => x.upgrade_run_id);
                    table.ForeignKey(
                        name: "FK_upgrade_run_installation_installation_id",
                        column: x => x.installation_id,
                        principalTable: "installation",
                        principalColumn: "installation_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "upgrade_run_step",
                columns: table => new
                {
                    upgrade_run_step_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    upgrade_run_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    step_key = table.Column<string>(type: "TEXT", nullable: false),
                    state = table.Column<string>(type: "TEXT", nullable: false),
                    attempt = table.Column<int>(type: "INTEGER", nullable: false),
                    next_retry_at_utc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    last_error_code = table.Column<string>(type: "TEXT", nullable: true),
                    last_error_message = table.Column<string>(type: "TEXT", nullable: true),
                    started_at_utc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    finished_at_utc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_upgrade_run_step", x => x.upgrade_run_step_id);
                    table.ForeignKey(
                        name: "FK_upgrade_run_step_upgrade_run_upgrade_run_id",
                        column: x => x.upgrade_run_id,
                        principalTable: "upgrade_run",
                        principalColumn: "upgrade_run_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_upgrade_run_installation_id",
                table: "upgrade_run",
                column: "installation_id");

            migrationBuilder.CreateIndex(
                name: "IX_upgrade_run_step_upgrade_run_id",
                table: "upgrade_run_step",
                column: "upgrade_run_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_log");

            migrationBuilder.DropTable(
                name: "upgrade_run_step");

            migrationBuilder.DropTable(
                name: "upgrade_run");

            migrationBuilder.DropTable(
                name: "installation");
        }
    }
}
