using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LowCodePlatform.Backend.Data.Migrations.PlatformSqlServer
{
    /// <inheritdoc />
    public partial class InitialPlatformSqlServer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "audit_log",
                columns: table => new
                {
                    audit_log_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    actor = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    action = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    target = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    installation_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    timestamp_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    trace_id = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    details_json = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_log", x => x.audit_log_id);
                });

            migrationBuilder.CreateTable(
                name: "entity_definition",
                columns: table => new
                {
                    entity_definition_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_entity_definition", x => x.entity_definition_id);
                });

            migrationBuilder.CreateTable(
                name: "installation",
                columns: table => new
                {
                    installation_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    release_channel = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    current_version = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    supported_version = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    release_date_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    upgrade_window_days = table.Column<int>(type: "int", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_installation", x => x.installation_id);
                });

            migrationBuilder.CreateTable(
                name: "workflow_definition",
                columns: table => new
                {
                    workflow_definition_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    definition_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    inbound_trigger_secret_sha256_hex = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    schedule_enabled = table.Column<bool>(type: "bit", nullable: false),
                    schedule_cron = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    schedule_next_due_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workflow_definition", x => x.workflow_definition_id);
                });

            migrationBuilder.CreateTable(
                name: "entity_record",
                columns: table => new
                {
                    entity_record_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    entity_definition_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    data_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_entity_record", x => x.entity_record_id);
                    table.ForeignKey(
                        name: "FK_entity_record_entity_definition_entity_definition_id",
                        column: x => x.entity_definition_id,
                        principalTable: "entity_definition",
                        principalColumn: "entity_definition_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "field_definition",
                columns: table => new
                {
                    field_definition_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    entity_definition_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    field_type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    is_required = table.Column<bool>(type: "bit", nullable: false),
                    max_length = table.Column<int>(type: "int", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_field_definition", x => x.field_definition_id);
                    table.ForeignKey(
                        name: "FK_field_definition_entity_definition_entity_definition_id",
                        column: x => x.entity_definition_id,
                        principalTable: "entity_definition",
                        principalColumn: "entity_definition_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "upgrade_run",
                columns: table => new
                {
                    upgrade_run_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    installation_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    target_version = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    state = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    started_at_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    finished_at_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    error_code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    error_message = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    trace_id = table.Column<string>(type: "nvarchar(max)", nullable: false)
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
                name: "workflow_run",
                columns: table => new
                {
                    workflow_run_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    workflow_definition_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    state = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    started_at_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    finished_at_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    trace_id = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    error_code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    error_message = table.Column<string>(type: "nvarchar(max)", nullable: true)
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
                name: "upgrade_run_step",
                columns: table => new
                {
                    upgrade_run_step_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    upgrade_run_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    step_key = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    state = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    attempt = table.Column<int>(type: "int", nullable: false),
                    next_retry_at_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    last_error_code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    last_error_message = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    started_at_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    finished_at_utc = table.Column<DateTime>(type: "datetime2", nullable: true)
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

            migrationBuilder.CreateTable(
                name: "workflow_step_run",
                columns: table => new
                {
                    workflow_step_run_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    workflow_run_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    step_key = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    step_type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    step_config_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    output_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    state = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    attempt = table.Column<int>(type: "int", nullable: false),
                    last_error_code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    last_error_message = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    last_error_config_path = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    started_at_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    finished_at_utc = table.Column<DateTime>(type: "datetime2", nullable: true)
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
                name: "IX_entity_definition_name",
                table: "entity_definition",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_entity_record_entity_definition_id",
                table: "entity_record",
                column: "entity_definition_id");

            migrationBuilder.CreateIndex(
                name: "IX_entity_record_updated_at_utc",
                table: "entity_record",
                column: "updated_at_utc");

            migrationBuilder.CreateIndex(
                name: "IX_field_definition_entity_definition_id",
                table: "field_definition",
                column: "entity_definition_id");

            migrationBuilder.CreateIndex(
                name: "IX_field_definition_entity_definition_id_name",
                table: "field_definition",
                columns: new[] { "entity_definition_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_upgrade_run_installation_id",
                table: "upgrade_run",
                column: "installation_id");

            migrationBuilder.CreateIndex(
                name: "IX_upgrade_run_step_upgrade_run_id",
                table: "upgrade_run_step",
                column: "upgrade_run_id");

            migrationBuilder.CreateIndex(
                name: "IX_workflow_definition_name",
                table: "workflow_definition",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "IX_workflow_definition_schedule_enabled_schedule_next_due_utc",
                table: "workflow_definition",
                columns: new[] { "schedule_enabled", "schedule_next_due_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_workflow_run_started_at_utc",
                table: "workflow_run",
                column: "started_at_utc");

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
                name: "audit_log");

            migrationBuilder.DropTable(
                name: "entity_record");

            migrationBuilder.DropTable(
                name: "field_definition");

            migrationBuilder.DropTable(
                name: "upgrade_run_step");

            migrationBuilder.DropTable(
                name: "workflow_step_run");

            migrationBuilder.DropTable(
                name: "entity_definition");

            migrationBuilder.DropTable(
                name: "upgrade_run");

            migrationBuilder.DropTable(
                name: "workflow_run");

            migrationBuilder.DropTable(
                name: "installation");

            migrationBuilder.DropTable(
                name: "workflow_definition");
        }
    }
}
