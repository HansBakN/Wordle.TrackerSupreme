using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wordle.TrackerSupreme.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DailyPuzzles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PuzzleDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Solution = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true),
                    IsArchived = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyPuzzles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Players",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now() at time zone 'utc'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Players", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Attempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    DailyPuzzleId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    CreatedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now() at time zone 'utc'"),
                    CompletedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PlayedInHardMode = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Attempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Attempts_DailyPuzzles_DailyPuzzleId",
                        column: x => x.DailyPuzzleId,
                        principalTable: "DailyPuzzles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Attempts_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Guesses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayerPuzzleAttemptId = table.Column<Guid>(type: "uuid", nullable: false),
                    GuessNumber = table.Column<int>(type: "integer", nullable: false),
                    GuessWord = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Guesses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Guesses_Attempts_PlayerPuzzleAttemptId",
                        column: x => x.PlayerPuzzleAttemptId,
                        principalTable: "Attempts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LetterEvaluations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GuessAttemptId = table.Column<Guid>(type: "uuid", nullable: false),
                    Letter = table.Column<char>(type: "character(1)", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    Result = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LetterEvaluations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LetterEvaluations_Guesses_GuessAttemptId",
                        column: x => x.GuessAttemptId,
                        principalTable: "Guesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Attempts_DailyPuzzleId",
                table: "Attempts",
                column: "DailyPuzzleId");

            migrationBuilder.CreateIndex(
                name: "IX_Attempts_PlayerId_DailyPuzzleId",
                table: "Attempts",
                columns: new[] { "PlayerId", "DailyPuzzleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DailyPuzzles_PuzzleDate",
                table: "DailyPuzzles",
                column: "PuzzleDate",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Guesses_PlayerPuzzleAttemptId_GuessNumber",
                table: "Guesses",
                columns: new[] { "PlayerPuzzleAttemptId", "GuessNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LetterEvaluations_GuessAttemptId_Position",
                table: "LetterEvaluations",
                columns: new[] { "GuessAttemptId", "Position" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LetterEvaluations");

            migrationBuilder.DropTable(
                name: "Guesses");

            migrationBuilder.DropTable(
                name: "Attempts");

            migrationBuilder.DropTable(
                name: "DailyPuzzles");

            migrationBuilder.DropTable(
                name: "Players");
        }
    }
}
