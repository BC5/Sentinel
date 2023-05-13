using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sentinel.Migrations
{
    /// <inheritdoc />
    public partial class AddFactcheckCost : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FactcheckCost",
                table: "Servers",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FactcheckCost",
                table: "Servers");
        }
    }
}
