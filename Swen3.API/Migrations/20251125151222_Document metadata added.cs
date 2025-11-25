using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Swen3.API.Migrations
{
    /// <inheritdoc />
    public partial class Documentmetadataadded : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Metadata",
                table: "Documents",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Metadata",
                table: "Documents");
        }
    }
}
