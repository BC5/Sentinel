using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sentinel.Migrations
{
    /// <inheritdoc />
    public partial class AddPurgeConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PurgeConfiguration",
                columns: table => new
                {
                    ConfigurationID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ChannelID = table.Column<ulong>(type: "INTEGER", nullable: false),
                    LastPurge = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ServerConfigDiscordID = table.Column<ulong>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurgeConfiguration", x => x.ConfigurationID);
                    table.ForeignKey(
                        name: "FK_PurgeConfiguration_Servers_ServerConfigDiscordID",
                        column: x => x.ServerConfigDiscordID,
                        principalTable: "Servers",
                        principalColumn: "DiscordID");
                });

            migrationBuilder.CreateIndex(
                name: "IX_PurgeConfiguration_ServerConfigDiscordID",
                table: "PurgeConfiguration",
                column: "ServerConfigDiscordID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PurgeConfiguration");
        }
    }
}
