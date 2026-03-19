using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LowCodePlatform.Backend.Data.Migrations.Platform
{
    /// <inheritdoc />
    public partial class AddEntityDefinitions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "entity_definition",
                columns: table => new
                {
                    entity_definition_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    name = table.Column<string>(type: "TEXT", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_entity_definition", x => x.entity_definition_id);
                });

            migrationBuilder.CreateTable(
                name: "field_definition",
                columns: table => new
                {
                    field_definition_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    entity_definition_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    name = table.Column<string>(type: "TEXT", nullable: false),
                    field_type = table.Column<string>(type: "TEXT", nullable: false),
                    is_required = table.Column<bool>(type: "INTEGER", nullable: false),
                    max_length = table.Column<int>(type: "INTEGER", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "TEXT", nullable: false)
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

            migrationBuilder.CreateIndex(
                name: "IX_entity_definition_name",
                table: "entity_definition",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_field_definition_entity_definition_id",
                table: "field_definition",
                column: "entity_definition_id");

            migrationBuilder.CreateIndex(
                name: "IX_field_definition_entity_definition_id_name",
                table: "field_definition",
                columns: new[] { "entity_definition_id", "name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "field_definition");

            migrationBuilder.DropTable(
                name: "entity_definition");
        }
    }
}
