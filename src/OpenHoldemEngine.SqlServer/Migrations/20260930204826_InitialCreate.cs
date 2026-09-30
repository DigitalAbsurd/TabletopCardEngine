using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenHoldemEngine.SqlServer.Migrations
{
    /// <inheritdoc />
    internal partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "ohe");

            migrationBuilder.CreateTable(
                name: "__OheSchema",
                schema: "ohe",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    SchemaVersion = table.Column<int>(type: "int", nullable: false),
                    EngineVersion = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK___OheSchema", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Game",
                schema: "ohe",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InitialSmallBlind = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MaxPlayersCount = table.Column<int>(type: "int", nullable: false),
                    InitialDealerSeatNo = table.Column<byte>(type: "tinyint", nullable: false),
                    GameType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    BettingStructure = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    IsFinished = table.Column<bool>(type: "bit", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Game", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BuyIn",
                schema: "ohe",
                columns: table => new
                {
                    GameId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SeatNo = table.Column<byte>(type: "tinyint", nullable: false),
                    HandNo = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BuyIn", x => new { x.GameId, x.SeatNo, x.HandNo });
                    table.ForeignKey(
                        name: "FK_BuyIn_Game_GameId",
                        column: x => x.GameId,
                        principalSchema: "ohe",
                        principalTable: "Game",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Hand",
                schema: "ohe",
                columns: table => new
                {
                    GameId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    No = table.Column<int>(type: "int", nullable: false),
                    DealerSeatNo = table.Column<byte>(type: "tinyint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Hand", x => new { x.GameId, x.No });
                    table.ForeignKey(
                        name: "FK_Hand_Game_GameId",
                        column: x => x.GameId,
                        principalSchema: "ohe",
                        principalTable: "Game",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Player",
                schema: "ohe",
                columns: table => new
                {
                    GameId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SeatNo = table.Column<byte>(type: "tinyint", nullable: false),
                    HandNoStart = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    HandNoEnd = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Player", x => new { x.GameId, x.SeatNo, x.HandNoStart });
                    table.ForeignKey(
                        name: "FK_Player_Game_GameId",
                        column: x => x.GameId,
                        principalSchema: "ohe",
                        principalTable: "Game",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HandAction",
                schema: "ohe",
                columns: table => new
                {
                    GameId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HandNo = table.Column<int>(type: "int", nullable: false),
                    No = table.Column<int>(type: "int", nullable: false),
                    SeatNo = table.Column<byte>(type: "tinyint", nullable: false),
                    NextSeatNo = table.Column<byte>(type: "tinyint", nullable: false),
                    Street = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    NextStreet = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ActionType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Size = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AddAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Paid = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HandAction", x => new { x.GameId, x.HandNo, x.No, x.SeatNo });
                    table.ForeignKey(
                        name: "FK_HandAction_Game_GameId",
                        column: x => x.GameId,
                        principalSchema: "ohe",
                        principalTable: "Game",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HandAction_Hand_GameId_HandNo",
                        columns: x => new { x.GameId, x.HandNo },
                        principalSchema: "ohe",
                        principalTable: "Hand",
                        principalColumns: new[] { "GameId", "No" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HandDeckCard",
                schema: "ohe",
                columns: table => new
                {
                    GameId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HandNo = table.Column<int>(type: "int", nullable: false),
                    Sequence = table.Column<byte>(type: "tinyint", nullable: false),
                    Card = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HandDeckCard", x => new { x.GameId, x.HandNo, x.Sequence });
                    table.ForeignKey(
                        name: "FK_HandDeckCard_Hand_GameId_HandNo",
                        columns: x => new { x.GameId, x.HandNo },
                        principalSchema: "ohe",
                        principalTable: "Hand",
                        principalColumns: new[] { "GameId", "No" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HandHoleCard",
                schema: "ohe",
                columns: table => new
                {
                    GameId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HandNo = table.Column<int>(type: "int", nullable: false),
                    SeatNo = table.Column<byte>(type: "tinyint", nullable: false),
                    Sequence = table.Column<byte>(type: "tinyint", nullable: false),
                    Card = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HandHoleCard", x => new { x.GameId, x.HandNo, x.SeatNo, x.Sequence });
                    table.ForeignKey(
                        name: "FK_HandHoleCard_Hand_GameId_HandNo",
                        columns: x => new { x.GameId, x.HandNo },
                        principalSchema: "ohe",
                        principalTable: "Hand",
                        principalColumns: new[] { "GameId", "No" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HandPlayerEndState",
                schema: "ohe",
                columns: table => new
                {
                    GameId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HandNo = table.Column<int>(type: "int", nullable: false),
                    SeatNo = table.Column<byte>(type: "tinyint", nullable: false),
                    FinishStack = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    WinAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Strength = table.Column<long>(type: "bigint", nullable: false),
                    RankType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HandPlayerEndState", x => new { x.GameId, x.HandNo, x.SeatNo });
                    table.ForeignKey(
                        name: "FK_HandPlayerEndState_Hand_GameId_HandNo",
                        columns: x => new { x.GameId, x.HandNo },
                        principalSchema: "ohe",
                        principalTable: "Hand",
                        principalColumns: new[] { "GameId", "No" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HandPlayerStartState",
                schema: "ohe",
                columns: table => new
                {
                    GameId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HandNo = table.Column<int>(type: "int", nullable: false),
                    SeatNo = table.Column<byte>(type: "tinyint", nullable: false),
                    StartStack = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HandPlayerStartState", x => new { x.GameId, x.HandNo, x.SeatNo });
                    table.ForeignKey(
                        name: "FK_HandPlayerStartState_Hand_GameId_HandNo",
                        columns: x => new { x.GameId, x.HandNo },
                        principalSchema: "ohe",
                        principalTable: "Hand",
                        principalColumns: new[] { "GameId", "No" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                schema: "ohe",
                table: "__OheSchema",
                columns: new[] { "Id", "EngineVersion", "SchemaVersion" },
                values: new object[] { 1, "1.0.0", 1 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "__OheSchema",
                schema: "ohe");

            migrationBuilder.DropTable(
                name: "BuyIn",
                schema: "ohe");

            migrationBuilder.DropTable(
                name: "HandAction",
                schema: "ohe");

            migrationBuilder.DropTable(
                name: "HandDeckCard",
                schema: "ohe");

            migrationBuilder.DropTable(
                name: "HandHoleCard",
                schema: "ohe");

            migrationBuilder.DropTable(
                name: "HandPlayerEndState",
                schema: "ohe");

            migrationBuilder.DropTable(
                name: "HandPlayerStartState",
                schema: "ohe");

            migrationBuilder.DropTable(
                name: "Player",
                schema: "ohe");

            migrationBuilder.DropTable(
                name: "Hand",
                schema: "ohe");

            migrationBuilder.DropTable(
                name: "Game",
                schema: "ohe");
        }
    }
}
