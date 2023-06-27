using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sentinel.Migrations
{
    /// <inheritdoc />
    public partial class FixSentenceTimespan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<long>(
                name: "DefaultSentence",
                table: "Servers",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(TimeSpan),
                oldType: "time(6)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<TimeSpan>(
                name: "DefaultSentence",
                table: "Servers",
                type: "time(6)",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");
        }
    }
}
