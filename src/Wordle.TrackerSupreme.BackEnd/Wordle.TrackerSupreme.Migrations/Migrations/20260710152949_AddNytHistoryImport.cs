using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wordle.TrackerSupreme.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddNytHistoryImport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "NytPuzzleId",
                table: "DailyPuzzles",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PublicPuzzleNumber",
                table: "DailyPuzzles",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "NytImportSessionId",
                table: "Attempts",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "NytImportSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    CodeHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AggregateGamesPlayed = table.Column<int>(type: "integer", nullable: true),
                    Requested = table.Column<int>(type: "integer", nullable: false),
                    Imported = table.Column<int>(type: "integer", nullable: false),
                    Duplicates = table.Column<int>(type: "integer", nullable: false),
                    Conflicts = table.Column<int>(type: "integer", nullable: false),
                    Rejected = table.Column<int>(type: "integer", nullable: false),
                    MissingFromNyt = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NytImportSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NytImportSessions_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DailyPuzzles_NytPuzzleId",
                table: "DailyPuzzles",
                column: "NytPuzzleId",
                unique: true,
                filter: "\"NytPuzzleId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Attempts_NytImportSessionId",
                table: "Attempts",
                column: "NytImportSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_NytImportSessions_CodeHash",
                table: "NytImportSessions",
                column: "CodeHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NytImportSessions_PlayerId_CreatedOn",
                table: "NytImportSessions",
                columns: new[] { "PlayerId", "CreatedOn" });

            migrationBuilder.AddForeignKey(
                name: "FK_Attempts_NytImportSessions_NytImportSessionId",
                table: "Attempts",
                column: "NytImportSessionId",
                principalTable: "NytImportSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Attempts_NytImportSessions_NytImportSessionId",
                table: "Attempts");

            migrationBuilder.DropTable(
                name: "NytImportSessions");

            migrationBuilder.DropIndex(
                name: "IX_DailyPuzzles_NytPuzzleId",
                table: "DailyPuzzles");

            migrationBuilder.DropIndex(
                name: "IX_Attempts_NytImportSessionId",
                table: "Attempts");

            migrationBuilder.DropColumn(
                name: "NytPuzzleId",
                table: "DailyPuzzles");

            migrationBuilder.DropColumn(
                name: "PublicPuzzleNumber",
                table: "DailyPuzzles");

            migrationBuilder.DropColumn(
                name: "NytImportSessionId",
                table: "Attempts");
        }
    }
}
