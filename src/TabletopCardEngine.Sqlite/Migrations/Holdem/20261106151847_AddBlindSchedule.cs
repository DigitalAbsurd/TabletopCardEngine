using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TabletopCardEngine.Sqlite.Migrations.Holdem
{
    /// <inheritdoc />
    internal partial class AddBlindSchedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BlindLevel",
                table: "tce_holdem_Hand",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BlindLevelStartHandNo",
                table: "tce_holdem_Hand",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BlindThresholdsPassed",
                table: "tce_holdem_Hand",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "SmallBlind",
                table: "tce_holdem_Hand",
                type: "TEXT",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BlindSchedule",
                table: "tce_holdem_Game",
                type: "TEXT",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "tce_holdem___TceSchema",
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
                table: "tce_holdem_Hand");

            migrationBuilder.DropColumn(
                name: "BlindLevelStartHandNo",
                table: "tce_holdem_Hand");

            migrationBuilder.DropColumn(
                name: "BlindThresholdsPassed",
                table: "tce_holdem_Hand");

            migrationBuilder.DropColumn(
                name: "SmallBlind",
                table: "tce_holdem_Hand");

            migrationBuilder.DropColumn(
                name: "BlindSchedule",
                table: "tce_holdem_Game");

            migrationBuilder.UpdateData(
                table: "tce_holdem___TceSchema",
                keyColumn: "Id",
                keyValue: 1,
                column: "SchemaVersion",
                value: 3);
        }
    }
}
