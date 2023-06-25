using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sentinel.Migrations
{
    /// <inheritdoc />
    public partial class AddReactBoard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReactBoardConfig",
                columns: table => new
                {
                    ReactConfigId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ChannelId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    Reaction = table.Column<string>(type: "TEXT", nullable: false),
                    Threshold = table.Column<uint>(type: "INTEGER", nullable: false),
                    ServerConfigDiscordID = table.Column<ulong>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReactBoardConfig", x => x.ReactConfigId);
                    table.ForeignKey(
                        name: "FK_ReactBoardConfig_Servers_ServerConfigDiscordID",
                        column: x => x.ServerConfigDiscordID,
                        principalTable: "Servers",
                        principalColumn: "DiscordID");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReactBoardConfig_ServerConfigDiscordID",
                table: "ReactBoardConfig",
                column: "ServerConfigDiscordID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReactBoardConfig");
        }
    }
}
