using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sentinel.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Ballots",
                columns: table => new
                {
                    BallotId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ElectionId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    VoterId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    Ballot = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ballots", x => x.BallotId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "OcrEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Server = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    Channel = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    Message = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    ImageURL = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ImageHash = table.Column<byte[]>(type: "longblob", nullable: false),
                    Text = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OcrEntries", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ReactLog",
                columns: table => new
                {
                    ReactionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    MessageId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    ReactorId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    ReacteeId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    ServerId = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    ReactName = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReactLog", x => x.ReactionId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Servers",
                columns: table => new
                {
                    DiscordID = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    FlagChannel = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    ModRole = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    MuteCost = table.Column<int>(type: "int", nullable: false),
                    NickCost = table.Column<int>(type: "int", nullable: false),
                    DeflectorCost = table.Column<int>(type: "int", nullable: false),
                    Cost1984 = table.Column<int>(type: "int", nullable: false),
                    CostDe1984 = table.Column<int>(type: "int", nullable: false),
                    CostWarn = table.Column<int>(type: "int", nullable: false),
                    FrenchCost = table.Column<int>(type: "int", nullable: false),
                    FactcheckCost = table.Column<int>(type: "int", nullable: false),
                    RewardChance = table.Column<float>(type: "float", nullable: false),
                    RewardSize = table.Column<int>(type: "int", nullable: false),
                    FunnyCommands = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IdiotRole = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    DefaultSentence = table.Column<TimeSpan>(type: "time(6)", nullable: false),
                    FrenchChannel = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    SlotsPayout = table.Column<int>(type: "int", nullable: false),
                    SlotsFee = table.Column<int>(type: "int", nullable: false),
                    DefaultRoles = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    GeneralChannel = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    IdiotChannel = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    ArrivalsChannel = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    LogChannel = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    ArrivalMessage = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ApprovalMessage = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Servers", x => x.DiscordID);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SocialCreditLog",
                columns: table => new
                {
                    EntryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    ServerId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    Points = table.Column<long>(type: "bigint", nullable: false),
                    Reason = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SocialCreditLog", x => x.EntryId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Transactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ServerID = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    RecipientID = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    SenderID = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    Amount = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transactions", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    CompositeID = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UserSnowflake = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    ServerSnowflake = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    Earnings = table.Column<int>(type: "int", nullable: false),
                    Balance = table.Column<int>(type: "int", nullable: false),
                    Multiplier = table.Column<float>(type: "float", nullable: false),
                    Nicklock = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SocialCredit = table.Column<long>(type: "bigint", nullable: false),
                    PrevNick = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NicklockUntil = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Authoritative = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Censored = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Immune = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Francophone = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Juvecheck = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    RoleBackup = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IdiotedUntil = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DeflectorExpiry = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Verified = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.CompositeID);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Votes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Poll = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    User = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Votes", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Warns",
                columns: table => new
                {
                    warnid = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    serverid = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    warner = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    warned = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    warnTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    warnReason = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Warns", x => x.warnid);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AutoResponse",
                columns: table => new
                {
                    ResponseId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Trigger = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TargetUser = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    TargetChannel = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    Wildcard = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ResponseText = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ResponseEmote = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Chance = table.Column<int>(type: "int", nullable: false),
                    RateLimit = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    LastTrigger = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ReloadTime = table.Column<TimeSpan>(type: "time(6)", nullable: true),
                    ServerConfigDiscordID = table.Column<ulong>(type: "bigint unsigned", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AutoResponse", x => x.ResponseId);
                    table.ForeignKey(
                        name: "FK_AutoResponse_Servers_ServerConfigDiscordID",
                        column: x => x.ServerConfigDiscordID,
                        principalTable: "Servers",
                        principalColumn: "DiscordID");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CensorEntry",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Phrase = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Requirement = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Wildcard = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ServerConfigDiscordID = table.Column<ulong>(type: "bigint unsigned", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CensorEntry", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CensorEntry_Servers_ServerConfigDiscordID",
                        column: x => x.ServerConfigDiscordID,
                        principalTable: "Servers",
                        principalColumn: "DiscordID");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PurgeConfiguration",
                columns: table => new
                {
                    ConfigurationID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ChannelID = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    LastPurge = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    ServerConfigDiscordID = table.Column<ulong>(type: "bigint unsigned", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurgeConfiguration", x => x.ConfigurationID);
                    table.ForeignKey(
                        name: "FK_PurgeConfiguration_Servers_ServerConfigDiscordID",
                        column: x => x.ServerConfigDiscordID,
                        principalTable: "Servers",
                        principalColumn: "DiscordID");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "QuoteEntry",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ServerId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    Text = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ServerConfigDiscordID = table.Column<ulong>(type: "bigint unsigned", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuoteEntry", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuoteEntry_Servers_ServerConfigDiscordID",
                        column: x => x.ServerConfigDiscordID,
                        principalTable: "Servers",
                        principalColumn: "DiscordID");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ReactBoardConfig",
                columns: table => new
                {
                    ReactConfigId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ChannelId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    Reaction = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Threshold = table.Column<uint>(type: "int unsigned", nullable: false),
                    ServerConfigDiscordID = table.Column<ulong>(type: "bigint unsigned", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReactBoardConfig", x => x.ReactConfigId);
                    table.ForeignKey(
                        name: "FK_ReactBoardConfig_Servers_ServerConfigDiscordID",
                        column: x => x.ServerConfigDiscordID,
                        principalTable: "Servers",
                        principalColumn: "DiscordID");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_AutoResponse_ServerConfigDiscordID",
                table: "AutoResponse",
                column: "ServerConfigDiscordID");

            migrationBuilder.CreateIndex(
                name: "IX_CensorEntry_ServerConfigDiscordID",
                table: "CensorEntry",
                column: "ServerConfigDiscordID");

            migrationBuilder.CreateIndex(
                name: "IX_PurgeConfiguration_ServerConfigDiscordID",
                table: "PurgeConfiguration",
                column: "ServerConfigDiscordID");

            migrationBuilder.CreateIndex(
                name: "IX_QuoteEntry_ServerConfigDiscordID",
                table: "QuoteEntry",
                column: "ServerConfigDiscordID");

            migrationBuilder.CreateIndex(
                name: "IX_ReactBoardConfig_ServerConfigDiscordID",
                table: "ReactBoardConfig",
                column: "ServerConfigDiscordID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AutoResponse");

            migrationBuilder.DropTable(
                name: "Ballots");

            migrationBuilder.DropTable(
                name: "CensorEntry");

            migrationBuilder.DropTable(
                name: "OcrEntries");

            migrationBuilder.DropTable(
                name: "PurgeConfiguration");

            migrationBuilder.DropTable(
                name: "QuoteEntry");

            migrationBuilder.DropTable(
                name: "ReactBoardConfig");

            migrationBuilder.DropTable(
                name: "ReactLog");

            migrationBuilder.DropTable(
                name: "SocialCreditLog");

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
