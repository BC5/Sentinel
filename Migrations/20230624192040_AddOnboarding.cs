using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sentinel.Migrations
{
    /// <inheritdoc />
    public partial class AddOnboarding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Verified",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ApprovalMessage",
                table: "Servers",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ArrivalMessage",
                table: "Servers",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<ulong>(
                name: "ArrivalsChannel",
                table: "Servers",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DefaultRoles",
                table: "Servers",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<ulong>(
                name: "GeneralChannel",
                table: "Servers",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<ulong>(
                name: "IdiotChannel",
                table: "Servers",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Verified",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ApprovalMessage",
                table: "Servers");

            migrationBuilder.DropColumn(
                name: "ArrivalMessage",
                table: "Servers");

            migrationBuilder.DropColumn(
                name: "ArrivalsChannel",
                table: "Servers");

            migrationBuilder.DropColumn(
                name: "DefaultRoles",
                table: "Servers");

            migrationBuilder.DropColumn(
                name: "GeneralChannel",
                table: "Servers");

            migrationBuilder.DropColumn(
                name: "IdiotChannel",
                table: "Servers");
        }
    }
}
