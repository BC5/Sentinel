using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sentinel.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OcrEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Server = table.Column<ulong>(type: "INTEGER", nullable: false),
                    Channel = table.Column<ulong>(type: "INTEGER", nullable: false),
                    Message = table.Column<ulong>(type: "INTEGER", nullable: false),
                    ImageURL = table.Column<string>(type: "TEXT", nullable: false),
                    ImageHash = table.Column<byte[]>(type: "BLOB", nullable: false),
                    Text = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OcrEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReactLog",
                columns: table => new
                {
                    ReactionId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MessageId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    ReactorId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    ReacteeId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    ServerId = table.Column<ulong>(type: "INTEGER", nullable: true),
                    ReactName = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReactLog", x => x.ReactionId);
                });

            migrationBuilder.CreateTable(
                name: "Servers",
                columns: table => new
                {
                    DiscordID = table.Column<ulong>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FlagChannel = table.Column<ulong>(type: "INTEGER", nullable: true),
                    ModRole = table.Column<ulong>(type: "INTEGER", nullable: true),
                    MuteCost = table.Column<int>(type: "INTEGER", nullable: false),
                    NickCost = table.Column<int>(type: "INTEGER", nullable: false),
                    DeflectorCost = table.Column<int>(type: "INTEGER", nullable: false),
                    Cost1984 = table.Column<int>(type: "INTEGER", nullable: false),
                    CostDe1984 = table.Column<int>(type: "INTEGER", nullable: false),
                    CostWarn = table.Column<int>(type: "INTEGER", nullable: false),
                    RewardChance = table.Column<float>(type: "REAL", nullable: false),
                    RewardSize = table.Column<int>(type: "INTEGER", nullable: false),
                    FunnyCommands = table.Column<bool>(type: "INTEGER", nullable: false),
                    IdiotRole = table.Column<ulong>(type: "INTEGER", nullable: true),
                    DefaultSentence = table.Column<TimeSpan>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Servers", x => x.DiscordID);
                });

            migrationBuilder.CreateTable(
                name: "Transactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ServerID = table.Column<ulong>(type: "INTEGER", nullable: false),
                    RecipientID = table.Column<ulong>(type: "INTEGER", nullable: false),
                    SenderID = table.Column<ulong>(type: "INTEGER", nullable: false),
                    Amount = table.Column<int>(type: "INTEGER", nullable: false),
                    Reason = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transactions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    CompositeID = table.Column<string>(type: "TEXT", nullable: false),
                    UserSnowflake = table.Column<ulong>(type: "INTEGER", nullable: false),
                    ServerSnowflake = table.Column<ulong>(type: "INTEGER", nullable: false),
                    Earnings = table.Column<int>(type: "INTEGER", nullable: false),
                    Balance = table.Column<int>(type: "INTEGER", nullable: false),
                    Multiplier = table.Column<float>(type: "REAL", nullable: false),
                    Nicklock = table.Column<string>(type: "TEXT", nullable: false),
                    PrevNick = table.Column<string>(type: "TEXT", nullable: true),
                    NicklockUntil = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Authoritative = table.Column<bool>(type: "INTEGER", nullable: false),
                    Censored = table.Column<bool>(type: "INTEGER", nullable: false),
                    Immune = table.Column<bool>(type: "INTEGER", nullable: false),
                    RoleBackup = table.Column<string>(type: "TEXT", nullable: false),
                    IdiotedUntil = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeflectorExpiry = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.CompositeID);
                });

            migrationBuilder.CreateTable(
                name: "Votes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Poll = table.Column<ulong>(type: "INTEGER", nullable: false),
                    User = table.Column<ulong>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Votes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Warns",
                columns: table => new
                {
                    warnid = table.Column<uint>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    serverid = table.Column<ulong>(type: "INTEGER", nullable: false),
                    warner = table.Column<ulong>(type: "INTEGER", nullable: false),
                    warned = table.Column<ulong>(type: "INTEGER", nullable: false),
                    warnTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    warnReason = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Warns", x => x.warnid);
                });

            migrationBuilder.CreateTable(
                name: "CensorEntry",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Phrase = table.Column<string>(type: "TEXT", nullable: false),
                    Requirement = table.Column<bool>(type: "INTEGER", nullable: false),
                    Wildcard = table.Column<bool>(type: "INTEGER", nullable: false),
                    ServerConfigDiscordID = table.Column<ulong>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CensorEntry", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CensorEntry_Servers_ServerConfigDiscordID",
                        column: x => x.ServerConfigDiscordID,
                        principalTable: "Servers",
                        principalColumn: "DiscordID");
                });

            migrationBuilder.CreateIndex(
                name: "IX_CensorEntry_ServerConfigDiscordID",
                table: "CensorEntry",
                column: "ServerConfigDiscordID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CensorEntry");

            migrationBuilder.DropTable(
                name: "OcrEntries");

            migrationBuilder.DropTable(
                name: "ReactLog");

            migrationBuilder.DropTable(
                name: "Transactions");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Votes");

            migrationBuilder.DropTable(
                name: "Warns");

            migrationBuilder.DropTable(
                name: "Servers");
        }
    }
}
