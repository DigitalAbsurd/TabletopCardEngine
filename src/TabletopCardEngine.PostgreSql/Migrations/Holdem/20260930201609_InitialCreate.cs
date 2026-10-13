using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TabletopCardEngine.PostgreSql.Migrations.Holdem
{
    /// <inheritdoc />
    internal partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "tce_holdem");

            migrationBuilder.CreateTable(
                name: "__TceSchema",
                schema: "tce_holdem",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    SchemaVersion = table.Column<int>(type: "integer", nullable: false),
                    EngineVersion = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK___TceSchema", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Game",
                schema: "tce_holdem",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InitialSmallBlind = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    MaxPlayersCount = table.Column<int>(type: "integer", nullable: false),
                    InitialDealerSeatNo = table.Column<byte>(type: "smallint", nullable: false),
                    GameType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    BettingStructure = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    IsFinished = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Game", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BuyIn",
                schema: "tce_holdem",
                columns: table => new
                {
                    GameId = table.Column<Guid>(type: "uuid", nullable: false),
                    SeatNo = table.Column<byte>(type: "smallint", nullable: false),
                    HandNo = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BuyIn", x => new { x.GameId, x.SeatNo, x.HandNo });
                    table.ForeignKey(
                        name: "FK_BuyIn_Game_GameId",
                        column: x => x.GameId,
                        principalSchema: "tce_holdem",
                        principalTable: "Game",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Hand",
                schema: "tce_holdem",
                columns: table => new
                {
                    GameId = table.Column<Guid>(type: "uuid", nullable: false),
                    No = table.Column<int>(type: "integer", nullable: false),
                    DealerSeatNo = table.Column<byte>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Hand", x => new { x.GameId, x.No });
                    table.ForeignKey(
                        name: "FK_Hand_Game_GameId",
                        column: x => x.GameId,
                        principalSchema: "tce_holdem",
                        principalTable: "Game",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Player",
                schema: "tce_holdem",
                columns: table => new
                {
                    GameId = table.Column<Guid>(type: "uuid", nullable: false),
                    SeatNo = table.Column<byte>(type: "smallint", nullable: false),
                    HandNoStart = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    HandNoEnd = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Player", x => new { x.GameId, x.SeatNo, x.HandNoStart });
                    table.ForeignKey(
                        name: "FK_Player_Game_GameId",
                        column: x => x.GameId,
                        principalSchema: "tce_holdem",
                        principalTable: "Game",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HandAction",
                schema: "tce_holdem",
                columns: table => new
                {
                    GameId = table.Column<Guid>(type: "uuid", nullable: false),
                    HandNo = table.Column<int>(type: "integer", nullable: false),
                    No = table.Column<int>(type: "integer", nullable: false),
                    SeatNo = table.Column<byte>(type: "smallint", nullable: false),
                    NextSeatNo = table.Column<byte>(type: "smallint", nullable: false),
                    Street = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    NextStreet = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ActionType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Size = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    AddAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Paid = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HandAction", x => new { x.GameId, x.HandNo, x.No, x.SeatNo });
                    table.ForeignKey(
                        name: "FK_HandAction_Game_GameId",
                        column: x => x.GameId,
                        principalSchema: "tce_holdem",
                        principalTable: "Game",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HandAction_Hand_GameId_HandNo",
                        columns: x => new { x.GameId, x.HandNo },
                        principalSchema: "tce_holdem",
                        principalTable: "Hand",
                        principalColumns: new[] { "GameId", "No" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HandDeckCard",
                schema: "tce_holdem",
                columns: table => new
                {
                    GameId = table.Column<Guid>(type: "uuid", nullable: false),
                    HandNo = table.Column<int>(type: "integer", nullable: false),
                    Sequence = table.Column<byte>(type: "smallint", nullable: false),
                    Card = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HandDeckCard", x => new { x.GameId, x.HandNo, x.Sequence });
                    table.ForeignKey(
                        name: "FK_HandDeckCard_Hand_GameId_HandNo",
                        columns: x => new { x.GameId, x.HandNo },
                        principalSchema: "tce_holdem",
                        principalTable: "Hand",
                        principalColumns: new[] { "GameId", "No" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HandHoleCard",
                schema: "tce_holdem",
                columns: table => new
                {
                    GameId = table.Column<Guid>(type: "uuid", nullable: false),
                    HandNo = table.Column<int>(type: "integer", nullable: false),
                    SeatNo = table.Column<byte>(type: "smallint", nullable: false),
                    Sequence = table.Column<byte>(type: "smallint", nullable: false),
                    Card = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HandHoleCard", x => new { x.GameId, x.HandNo, x.SeatNo, x.Sequence });
                    table.ForeignKey(
                        name: "FK_HandHoleCard_Hand_GameId_HandNo",
                        columns: x => new { x.GameId, x.HandNo },
                        principalSchema: "tce_holdem",
                        principalTable: "Hand",
                        principalColumns: new[] { "GameId", "No" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HandPlayerEndState",
                schema: "tce_holdem",
                columns: table => new
                {
                    GameId = table.Column<Guid>(type: "uuid", nullable: false),
                    HandNo = table.Column<int>(type: "integer", nullable: false),
                    SeatNo = table.Column<byte>(type: "smallint", nullable: false),
                    FinishStack = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    WinAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Strength = table.Column<long>(type: "bigint", nullable: false),
                    RankType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Description = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HandPlayerEndState", x => new { x.GameId, x.HandNo, x.SeatNo });
                    table.ForeignKey(
                        name: "FK_HandPlayerEndState_Hand_GameId_HandNo",
                        columns: x => new { x.GameId, x.HandNo },
                        principalSchema: "tce_holdem",
                        principalTable: "Hand",
                        principalColumns: new[] { "GameId", "No" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HandPlayerStartState",
                schema: "tce_holdem",
                columns: table => new
                {
                    GameId = table.Column<Guid>(type: "uuid", nullable: false),
                    HandNo = table.Column<int>(type: "integer", nullable: false),
                    SeatNo = table.Column<byte>(type: "smallint", nullable: false),
                    StartStack = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HandPlayerStartState", x => new { x.GameId, x.HandNo, x.SeatNo });
                    table.ForeignKey(
                        name: "FK_HandPlayerStartState_Hand_GameId_HandNo",
                        columns: x => new { x.GameId, x.HandNo },
                        principalSchema: "tce_holdem",
                        principalTable: "Hand",
                        principalColumns: new[] { "GameId", "No" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                schema: "tce_holdem",
                table: "__TceSchema",
                columns: new[] { "Id", "EngineVersion", "SchemaVersion" },
                values: new object[] { 1, "1.0.0", 1 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "__TceSchema",
                schema: "tce_holdem");

            migrationBuilder.DropTable(
                name: "BuyIn",
                schema: "tce_holdem");

            migrationBuilder.DropTable(
                name: "HandAction",
                schema: "tce_holdem");

            migrationBuilder.DropTable(
                name: "HandDeckCard",
                schema: "tce_holdem");

            migrationBuilder.DropTable(
                name: "HandHoleCard",
                schema: "tce_holdem");

            migrationBuilder.DropTable(
                name: "HandPlayerEndState",
                schema: "tce_holdem");

            migrationBuilder.DropTable(
                name: "HandPlayerStartState",
                schema: "tce_holdem");

            migrationBuilder.DropTable(
                name: "Player",
                schema: "tce_holdem");

            migrationBuilder.DropTable(
                name: "Hand",
                schema: "tce_holdem");

            migrationBuilder.DropTable(
                name: "Game",
                schema: "tce_holdem");
        }
    }
}
