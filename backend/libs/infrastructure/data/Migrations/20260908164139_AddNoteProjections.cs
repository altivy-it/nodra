using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Altivy.Backend.Infra.Data.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddNoteProjections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "note_projections",
                schema: "altivy",
                columns: table => new
                {
                    vault_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    path = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    content_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    title = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    properties = table.Column<string>(type: "jsonb", nullable: false),
                    tags = table.Column<string>(type: "jsonb", nullable: false),
                    headings = table.Column<string>(type: "jsonb", nullable: false),
                    wiki_links = table.Column<string>(type: "jsonb", nullable: false),
                    markdown_links = table.Column<string>(type: "jsonb", nullable: false),
                    indexed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_note_projections", x => new { x.vault_id, x.path });
                });

            migrationBuilder.CreateIndex(
                name: "IX_note_projections_vault_id_title",
                schema: "altivy",
                table: "note_projections",
                columns: new[] { "vault_id", "title" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "note_projections",
                schema: "altivy");
        }
    }
}
