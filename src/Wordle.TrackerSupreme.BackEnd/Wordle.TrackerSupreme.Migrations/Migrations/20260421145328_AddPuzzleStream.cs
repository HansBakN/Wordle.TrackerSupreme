using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wordle.TrackerSupreme.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddPuzzleStream : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Stream",
                table: "DailyPuzzles",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "DailyPuzzles"
                SET "Stream" = 1
                WHERE "Stream" IS NULL;
                """);

            migrationBuilder.AlterColumn<int>(
                name: "Stream",
                table: "DailyPuzzles",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.DropIndex(
                name: "IX_DailyPuzzles_PuzzleDate",
                table: "DailyPuzzles");

            migrationBuilder.CreateIndex(
                name: "IX_DailyPuzzles_PuzzleDate_Stream",
                table: "DailyPuzzles",
                columns: new[] { "PuzzleDate", "Stream" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DailyPuzzles_PuzzleDate_Stream",
                table: "DailyPuzzles");

            migrationBuilder.DropColumn(
                name: "Stream",
                table: "DailyPuzzles");

            migrationBuilder.CreateIndex(
                name: "IX_DailyPuzzles_PuzzleDate",
                table: "DailyPuzzles",
                column: "PuzzleDate",
                unique: true);
        }
    }
}
