using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TabletopCardEngine.PostgreSql.Migrations.Holdem
{
    /// <inheritdoc />
    internal partial class AddBlindSchedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BlindLevel",
                schema: "tce_holdem",
                table: "Hand",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BlindLevelStartHandNo",
                schema: "tce_holdem",
                table: "Hand",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BlindThresholdsPassed",
                schema: "tce_holdem",
                table: "Hand",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "SmallBlind",
                schema: "tce_holdem",
                table: "Hand",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BlindSchedule",
                schema: "tce_holdem",
                table: "Game",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.UpdateData(
                schema: "tce_holdem",
                table: "__TceSchema",
                keyColumn: "Id",
                keyValue: 1,
                column: "SchemaVersion",
                value: 4);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BlindLevel",
                schema: "tce_holdem",
                table: "Hand");

            migrationBuilder.DropColumn(
                name: "BlindLevelStartHandNo",
                schema: "tce_holdem",
                table: "Hand");

            migrationBuilder.DropColumn(
                name: "BlindThresholdsPassed",
                schema: "tce_holdem",
                table: "Hand");

            migrationBuilder.DropColumn(
                name: "SmallBlind",
                schema: "tce_holdem",
                table: "Hand");

            migrationBuilder.DropColumn(
                name: "BlindSchedule",
                schema: "tce_holdem",
                table: "Game");

            migrationBuilder.UpdateData(
                schema: "tce_holdem",
                table: "__TceSchema",
                keyColumn: "Id",
                keyValue: 1,
                column: "SchemaVersion",
                value: 3);
        }
    }
}
