using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TabletopCardEngine.Sqlite.Migrations.Holdem
{
    /// <inheritdoc />
    internal partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tce_holdem___TceSchema",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    SchemaVersion = table.Column<int>(type: "INTEGER", nullable: false),
                    EngineVersion = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tce_holdem___TceSchema", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tce_holdem_Game",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    InitialSmallBlind = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    MaxPlayersCount = table.Column<int>(type: "INTEGER", nullable: false),
                    InitialDealerSeatNo = table.Column<byte>(type: "INTEGER", nullable: false),
                    GameType = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    BettingStructure = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    IsFinished = table.Column<bool>(type: "INTEGER", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tce_holdem_Game", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tce_holdem_BuyIn",
                columns: table => new
                {
                    GameId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SeatNo = table.Column<byte>(type: "INTEGER", nullable: false),
                    HandNo = table.Column<int>(type: "INTEGER", nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tce_holdem_BuyIn", x => new { x.GameId, x.SeatNo, x.HandNo });
                    table.ForeignKey(
                        name: "FK_tce_holdem_BuyIn_tce_holdem_Game_GameId",
                        column: x => x.GameId,
                        principalTable: "tce_holdem_Game",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tce_holdem_Hand",
                columns: table => new
                {
                    GameId = table.Column<Guid>(type: "TEXT", nullable: false),
                    No = table.Column<int>(type: "INTEGER", nullable: false),
                    DealerSeatNo = table.Column<byte>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tce_holdem_Hand", x => new { x.GameId, x.No });
                    table.ForeignKey(
                        name: "FK_tce_holdem_Hand_tce_holdem_Game_GameId",
                        column: x => x.GameId,
                        principalTable: "tce_holdem_Game",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tce_holdem_Player",
                columns: table => new
                {
                    GameId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SeatNo = table.Column<byte>(type: "INTEGER", nullable: false),
                    HandNoStart = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    HandNoEnd = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tce_holdem_Player", x => new { x.GameId, x.SeatNo, x.HandNoStart });
                    table.ForeignKey(
                        name: "FK_tce_holdem_Player_tce_holdem_Game_GameId",
                        column: x => x.GameId,
                        principalTable: "tce_holdem_Game",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tce_holdem_HandAction",
                columns: table => new
                {
                    GameId = table.Column<Guid>(type: "TEXT", nullable: false),
                    HandNo = table.Column<int>(type: "INTEGER", nullable: false),
                    No = table.Column<int>(type: "INTEGER", nullable: false),
                    SeatNo = table.Column<byte>(type: "INTEGER", nullable: false),
                    NextSeatNo = table.Column<byte>(type: "INTEGER", nullable: false),
                    Street = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    NextStreet = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    ActionType = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Size = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    AddAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Paid = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tce_holdem_HandAction", x => new { x.GameId, x.HandNo, x.No, x.SeatNo });
                    table.ForeignKey(
                        name: "FK_tce_holdem_HandAction_tce_holdem_Game_GameId",
                        column: x => x.GameId,
                        principalTable: "tce_holdem_Game",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tce_holdem_HandAction_tce_holdem_Hand_GameId_HandNo",
                        columns: x => new { x.GameId, x.HandNo },
                        principalTable: "tce_holdem_Hand",
                        principalColumns: new[] { "GameId", "No" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tce_holdem_HandDeckCard",
                columns: table => new
                {
                    GameId = table.Column<Guid>(type: "TEXT", nullable: false),
                    HandNo = table.Column<int>(type: "INTEGER", nullable: false),
                    Sequence = table.Column<byte>(type: "INTEGER", nullable: false),
                    Card = table.Column<string>(type: "TEXT", maxLength: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tce_holdem_HandDeckCard", x => new { x.GameId, x.HandNo, x.Sequence });
                    table.ForeignKey(
                        name: "FK_tce_holdem_HandDeckCard_tce_holdem_Hand_GameId_HandNo",
                        columns: x => new { x.GameId, x.HandNo },
                        principalTable: "tce_holdem_Hand",
                        principalColumns: new[] { "GameId", "No" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tce_holdem_HandHoleCard",
                columns: table => new
                {
                    GameId = table.Column<Guid>(type: "TEXT", nullable: false),
                    HandNo = table.Column<int>(type: "INTEGER", nullable: false),
                    SeatNo = table.Column<byte>(type: "INTEGER", nullable: false),
                    Sequence = table.Column<byte>(type: "INTEGER", nullable: false),
                    Card = table.Column<string>(type: "TEXT", maxLength: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tce_holdem_HandHoleCard", x => new { x.GameId, x.HandNo, x.SeatNo, x.Sequence });
                    table.ForeignKey(
                        name: "FK_tce_holdem_HandHoleCard_tce_holdem_Hand_GameId_HandNo",
                        columns: x => new { x.GameId, x.HandNo },
                        principalTable: "tce_holdem_Hand",
                        principalColumns: new[] { "GameId", "No" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tce_holdem_HandPlayerEndState",
                columns: table => new
                {
                    GameId = table.Column<Guid>(type: "TEXT", nullable: false),
                    HandNo = table.Column<int>(type: "INTEGER", nullable: false),
                    SeatNo = table.Column<byte>(type: "INTEGER", nullable: false),
                    FinishStack = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    WinAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Strength = table.Column<uint>(type: "INTEGER", nullable: false),
                    RankType = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tce_holdem_HandPlayerEndState", x => new { x.GameId, x.HandNo, x.SeatNo });
                    table.ForeignKey(
                        name: "FK_tce_holdem_HandPlayerEndState_tce_holdem_Hand_GameId_HandNo",
                        columns: x => new { x.GameId, x.HandNo },
                        principalTable: "tce_holdem_Hand",
                        principalColumns: new[] { "GameId", "No" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tce_holdem_HandPlayerStartState",
                columns: table => new
                {
                    GameId = table.Column<Guid>(type: "TEXT", nullable: false),
                    HandNo = table.Column<int>(type: "INTEGER", nullable: false),
                    SeatNo = table.Column<byte>(type: "INTEGER", nullable: false),
                    StartStack = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tce_holdem_HandPlayerStartState", x => new { x.GameId, x.HandNo, x.SeatNo });
                    table.ForeignKey(
                        name: "FK_tce_holdem_HandPlayerStartState_tce_holdem_Hand_GameId_HandNo",
                        columns: x => new { x.GameId, x.HandNo },
                        principalTable: "tce_holdem_Hand",
                        principalColumns: new[] { "GameId", "No" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "tce_holdem___TceSchema",
                columns: new[] { "Id", "EngineVersion", "SchemaVersion" },
                values: new object[] { 1, "1.0.0", 1 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tce_holdem___TceSchema");

            migrationBuilder.DropTable(
                name: "tce_holdem_BuyIn");

            migrationBuilder.DropTable(
                name: "tce_holdem_HandAction");

            migrationBuilder.DropTable(
                name: "tce_holdem_HandDeckCard");

            migrationBuilder.DropTable(
                name: "tce_holdem_HandHoleCard");

            migrationBuilder.DropTable(
                name: "tce_holdem_HandPlayerEndState");

            migrationBuilder.DropTable(
                name: "tce_holdem_HandPlayerStartState");

            migrationBuilder.DropTable(
                name: "tce_holdem_Player");

            migrationBuilder.DropTable(
                name: "tce_holdem_Hand");

            migrationBuilder.DropTable(
                name: "tce_holdem_Game");
        }
    }
}
