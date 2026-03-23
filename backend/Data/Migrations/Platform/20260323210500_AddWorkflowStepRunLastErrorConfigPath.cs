using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LowCodePlatform.Backend.Data.Migrations.Platform
{
    /// <inheritdoc />
    public partial class AddWorkflowStepRunLastErrorConfigPath : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "last_error_config_path",
                table: "workflow_step_run",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "last_error_config_path",
                table: "workflow_step_run");
        }
    }
}
