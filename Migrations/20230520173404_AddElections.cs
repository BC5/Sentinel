using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sentinel.Migrations
{
    /// <inheritdoc />
    public partial class AddElections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Ballots",
                columns: table => new
                {
                    BallotId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ElectionId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    VoterId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    Ballot = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ballots", x => x.BallotId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Ballots");
        }
    }
}
