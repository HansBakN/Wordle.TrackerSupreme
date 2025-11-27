using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BowlingTrackerSupreme.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddedGeneratedCreatedOn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DateCreated",
                table: "Games");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedOn",
                table: "Games",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_DATE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedOn",
                table: "Games");

            migrationBuilder.AddColumn<DateTime>(
                name: "DateCreated",
                table: "Games",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }
    }
}
