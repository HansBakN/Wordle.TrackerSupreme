using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wordle.TrackerSupreme.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddImportedNytArchiveFlag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsImportedArchive",
                table: "Attempts",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsImportedArchive",
                table: "Attempts");
        }
    }
}
