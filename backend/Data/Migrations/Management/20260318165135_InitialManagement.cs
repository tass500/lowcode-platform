using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LowCodePlatform.Backend.Data.Migrations.Management
{
    /// <inheritdoc />
    public partial class InitialManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tenant",
                columns: table => new
                {
                    tenant_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    slug = table.Column<string>(type: "TEXT", nullable: false),
                    connection_string = table.Column<string>(type: "TEXT", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenant", x => x.tenant_id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tenant_slug",
                table: "tenant",
                column: "slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tenant");
        }
    }
}
