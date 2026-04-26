using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wordle.TrackerSupreme.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddPracticeMode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPractice",
                table: "DailyPuzzles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.DropIndex(
                name: "IX_DailyPuzzles_PuzzleDate",
                table: "DailyPuzzles");

            migrationBuilder.CreateIndex(
                name: "IX_DailyPuzzles_PuzzleDate",
                table: "DailyPuzzles",
                column: "PuzzleDate",
                unique: true,
                filter: "\"IsPractice\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DailyPuzzles_PuzzleDate",
                table: "DailyPuzzles");

            migrationBuilder.DropColumn(
                name: "IsPractice",
                table: "DailyPuzzles");

            migrationBuilder.CreateIndex(
                name: "IX_DailyPuzzles_PuzzleDate",
                table: "DailyPuzzles",
                column: "PuzzleDate",
                unique: true);
        }
    }
}
