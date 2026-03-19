using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LowCodePlatform.Backend.Data.Migrations.Platform
{
    /// <inheritdoc />
    public partial class AddWorkflowDefinition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "workflow_definition",
                columns: table => new
                {
                    workflow_definition_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    name = table.Column<string>(type: "TEXT", nullable: false),
                    definition_json = table.Column<string>(type: "TEXT", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workflow_definition", x => x.workflow_definition_id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_workflow_definition_name",
                table: "workflow_definition",
                column: "name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "workflow_definition");
        }
    }
}
