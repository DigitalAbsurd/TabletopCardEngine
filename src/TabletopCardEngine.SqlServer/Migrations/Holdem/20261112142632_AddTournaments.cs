using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TabletopCardEngine.SqlServer.Migrations.Holdem
{
    /// <inheritdoc />
    internal partial class AddTournaments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Tournament",
                schema: "tce_holdem",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BettingStructure = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    TableFormat = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    MaxPlayersPerTable = table.Column<int>(type: "int", nullable: false),
                    RaiseCap = table.Column<int>(type: "int", nullable: true),
                    AnteType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    StartingStack = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    BuyIn = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MinEntries = table.Column<int>(type: "int", nullable: false),
                    MaxEntries = table.Column<int>(type: "int", nullable: true),
                    Levels = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Payouts = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    State = table.Column<byte>(type: "tinyint", nullable: false),
                    SeatDrawSeed = table.Column<int>(type: "int", nullable: true),
                    StartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    FinishedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Version = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tournament", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TournamentEntry",
                schema: "tce_holdem",
                columns: table => new
                {
                    TournamentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntryNo = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    TableNo = table.Column<int>(type: "int", nullable: true),
                    SeatNo = table.Column<byte>(type: "tinyint", nullable: true),
                    CarriedStack = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Place = table.Column<int>(type: "int", nullable: true),
                    Prize = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    EliminatedInHandNo = table.Column<int>(type: "int", nullable: true),
                    EliminatedAtTableNo = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TournamentEntry", x => new { x.TournamentId, x.EntryNo });
                    table.ForeignKey(
                        name: "FK_TournamentEntry_Tournament_TournamentId",
                        column: x => x.TournamentId,
                        principalSchema: "tce_holdem",
                        principalTable: "Tournament",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TournamentTable",
                schema: "tce_holdem",
                columns: table => new
                {
                    TournamentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TableNo = table.Column<int>(type: "int", nullable: false),
                    GameId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsBroken = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TournamentTable", x => new { x.TournamentId, x.TableNo });
                    table.ForeignKey(
                        name: "FK_TournamentTable_Game_GameId",
                        column: x => x.GameId,
                        principalSchema: "tce_holdem",
                        principalTable: "Game",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TournamentTable_Tournament_TournamentId",
                        column: x => x.TournamentId,
                        principalSchema: "tce_holdem",
                        principalTable: "Tournament",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                schema: "tce_holdem",
                table: "__TceSchema",
                keyColumn: "Id",
                keyValue: 1,
                column: "SchemaVersion",
                value: 7);

            migrationBuilder.CreateIndex(
                name: "IX_TournamentTable_GameId",
                schema: "tce_holdem",
                table: "TournamentTable",
                column: "GameId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TournamentEntry",
                schema: "tce_holdem");

            migrationBuilder.DropTable(
                name: "TournamentTable",
                schema: "tce_holdem");

            migrationBuilder.DropTable(
                name: "Tournament",
                schema: "tce_holdem");

            migrationBuilder.UpdateData(
                schema: "tce_holdem",
                table: "__TceSchema",
                keyColumn: "Id",
                keyValue: 1,
                column: "SchemaVersion",
                value: 6);
        }
    }
}
