using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LowCodePlatform.Backend.Data.Migrations.Platform
{
    /// <inheritdoc />
    public partial class AddEntityRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "entity_record",
                columns: table => new
                {
                    entity_record_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    entity_definition_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    data_json = table.Column<string>(type: "TEXT", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "TEXT", nullable: false)
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

            migrationBuilder.CreateIndex(
                name: "IX_entity_record_entity_definition_id",
                table: "entity_record",
                column: "entity_definition_id");

            migrationBuilder.CreateIndex(
                name: "IX_entity_record_updated_at_utc",
                table: "entity_record",
                column: "updated_at_utc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "entity_record");
        }
    }
}
