using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sentinel.Migrations
{
    /// <inheritdoc />
    public partial class AddAutoResponses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AutoResponse",
                columns: table => new
                {
                    ResponseId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Trigger = table.Column<string>(type: "TEXT", nullable: false),
                    TargetUser = table.Column<ulong>(type: "INTEGER", nullable: true),
                    TargetChannel = table.Column<ulong>(type: "INTEGER", nullable: true),
                    Wildcard = table.Column<bool>(type: "INTEGER", nullable: false),
                    ResponseText = table.Column<string>(type: "TEXT", nullable: true),
                    ResponseEmote = table.Column<string>(type: "TEXT", nullable: true),
                    Chance = table.Column<int>(type: "INTEGER", nullable: false),
                    RateLimit = table.Column<bool>(type: "INTEGER", nullable: false),
                    LastTrigger = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ReloadTime = table.Column<TimeSpan>(type: "TEXT", nullable: true),
                    ServerConfigDiscordID = table.Column<ulong>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AutoResponse", x => x.ResponseId);
                    table.ForeignKey(
                        name: "FK_AutoResponse_Servers_ServerConfigDiscordID",
                        column: x => x.ServerConfigDiscordID,
                        principalTable: "Servers",
                        principalColumn: "DiscordID");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AutoResponse_ServerConfigDiscordID",
                table: "AutoResponse",
                column: "ServerConfigDiscordID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AutoResponse");
        }
    }
}
