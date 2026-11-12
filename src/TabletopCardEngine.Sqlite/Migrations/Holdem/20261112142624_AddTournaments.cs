using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TabletopCardEngine.Sqlite.Migrations.Holdem
{
    /// <inheritdoc />
    internal partial class AddTournaments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tce_holdem_Tournament",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    BettingStructure = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    TableFormat = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    MaxPlayersPerTable = table.Column<int>(type: "INTEGER", nullable: false),
                    RaiseCap = table.Column<int>(type: "INTEGER", nullable: true),
                    AnteType = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    StartingStack = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    BuyIn = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    MinEntries = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxEntries = table.Column<int>(type: "INTEGER", nullable: true),
                    Levels = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    Payouts = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    State = table.Column<byte>(type: "INTEGER", nullable: false),
                    SeatDrawSeed = table.Column<int>(type: "INTEGER", nullable: true),
                    StartedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    FinishedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    Version = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tce_holdem_Tournament", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tce_holdem_TournamentEntry",
                columns: table => new
                {
                    TournamentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EntryNo = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    TableNo = table.Column<int>(type: "INTEGER", nullable: true),
                    SeatNo = table.Column<byte>(type: "INTEGER", nullable: true),
                    CarriedStack = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    Place = table.Column<int>(type: "INTEGER", nullable: true),
                    Prize = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    EliminatedInHandNo = table.Column<int>(type: "INTEGER", nullable: true),
                    EliminatedAtTableNo = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tce_holdem_TournamentEntry", x => new { x.TournamentId, x.EntryNo });
                    table.ForeignKey(
                        name: "FK_tce_holdem_TournamentEntry_tce_holdem_Tournament_TournamentId",
                        column: x => x.TournamentId,
                        principalTable: "tce_holdem_Tournament",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tce_holdem_TournamentTable",
                columns: table => new
                {
                    TournamentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TableNo = table.Column<int>(type: "INTEGER", nullable: false),
                    GameId = table.Column<Guid>(type: "TEXT", nullable: false),
                    IsBroken = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tce_holdem_TournamentTable", x => new { x.TournamentId, x.TableNo });
                    table.ForeignKey(
                        name: "FK_tce_holdem_TournamentTable_tce_holdem_Game_GameId",
                        column: x => x.GameId,
                        principalTable: "tce_holdem_Game",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tce_holdem_TournamentTable_tce_holdem_Tournament_TournamentId",
                        column: x => x.TournamentId,
                        principalTable: "tce_holdem_Tournament",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "tce_holdem___TceSchema",
                keyColumn: "Id",
                keyValue: 1,
                column: "SchemaVersion",
                value: 7);

            migrationBuilder.CreateIndex(
                name: "IX_tce_holdem_TournamentTable_GameId",
                table: "tce_holdem_TournamentTable",
                column: "GameId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tce_holdem_TournamentEntry");

            migrationBuilder.DropTable(
                name: "tce_holdem_TournamentTable");

            migrationBuilder.DropTable(
                name: "tce_holdem_Tournament");

            migrationBuilder.UpdateData(
                table: "tce_holdem___TceSchema",
                keyColumn: "Id",
                keyValue: 1,
                column: "SchemaVersion",
                value: 6);
        }
    }
}
