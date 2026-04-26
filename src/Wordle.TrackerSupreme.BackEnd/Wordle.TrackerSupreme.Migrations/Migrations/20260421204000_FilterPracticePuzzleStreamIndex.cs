using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wordle.TrackerSupreme.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class FilterPracticePuzzleStreamIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DailyPuzzles_PuzzleDate_Stream",
                table: "DailyPuzzles");

            migrationBuilder.CreateIndex(
                name: "IX_DailyPuzzles_PuzzleDate_Stream",
                table: "DailyPuzzles",
                columns: new[] { "PuzzleDate", "Stream" },
                unique: true,
                filter: "\"IsPractice\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DailyPuzzles_PuzzleDate_Stream",
                table: "DailyPuzzles");

            migrationBuilder.CreateIndex(
                name: "IX_DailyPuzzles_PuzzleDate_Stream",
                table: "DailyPuzzles",
                columns: new[] { "PuzzleDate", "Stream" },
                unique: true);
        }
    }
}
