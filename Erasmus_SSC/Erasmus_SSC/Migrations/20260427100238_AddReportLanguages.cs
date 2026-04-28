using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Erasmus_SSC.Migrations
{
    /// <inheritdoc />
    public partial class AddReportLanguages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "language_id",
                table: "Reports",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ReportLanguages",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false),
                    code = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_report_languages", x => x.id);
                });

            migrationBuilder.InsertData(
                table: "ReportLanguages",
                columns: new[] { "id", "code", "name" },
                values: new object[,]
                {
                    { 1, "en", "English" },
                    { 2, "da", "Danish" },
                    { 3, "no", "Norwegian" },
                    { 4, "nl", "Dutch" },
                    { 5, "fi", "Finnish" },
                    { 6, "et", "Estonian" }
                });

            migrationBuilder.CreateIndex(
                name: "ix_reports_language_id",
                table: "Reports",
                column: "language_id");

            migrationBuilder.AddForeignKey(
                name: "fk_reports_report_languages_language_id",
                table: "Reports",
                column: "language_id",
                principalTable: "ReportLanguages",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_reports_report_languages_language_id",
                table: "Reports");

            migrationBuilder.DropTable(
                name: "ReportLanguages");

            migrationBuilder.DropIndex(
                name: "ix_reports_language_id",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "language_id",
                table: "Reports");
        }
    }
}
