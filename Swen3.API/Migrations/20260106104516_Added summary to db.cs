using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Swen3.API.Migrations
{
    /// <inheritdoc />
    public partial class Addedsummarytodb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Summary",
                table: "Documents",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Summary",
                table: "Documents");
        }
    }
}
