using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sentinel.Migrations
{
    /// <inheritdoc />
    public partial class AddQuotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "QuoteEntry",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ServerId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    Text = table.Column<string>(type: "TEXT", nullable: false),
                    ServerConfigDiscordID = table.Column<ulong>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuoteEntry", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuoteEntry_Servers_ServerConfigDiscordID",
                        column: x => x.ServerConfigDiscordID,
                        principalTable: "Servers",
                        principalColumn: "DiscordID");
                });

            migrationBuilder.CreateIndex(
                name: "IX_QuoteEntry_ServerConfigDiscordID",
                table: "QuoteEntry",
                column: "ServerConfigDiscordID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QuoteEntry");
        }
    }
}
