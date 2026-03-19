using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LowCodePlatform.Backend.Data.Migrations.Management
{
    /// <inheritdoc />
    public partial class AddTenantConnectionStringSecretRef : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "connection_string",
                table: "tenant",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AddColumn<string>(
                name: "connection_string_secret_ref",
                table: "tenant",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "connection_string_secret_ref",
                table: "tenant");

            migrationBuilder.AlterColumn<string>(
                name: "connection_string",
                table: "tenant",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);
        }
    }
}
