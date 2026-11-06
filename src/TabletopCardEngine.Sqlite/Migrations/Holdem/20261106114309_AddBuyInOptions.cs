using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TabletopCardEngine.Sqlite.Migrations.Holdem
{
    /// <inheritdoc />
    internal partial class AddBuyInOptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AllowRebuy",
                table: "tce_holdem_Game",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AllowTopUp",
                table: "tce_holdem_Game",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "MaxBuyIn",
                table: "tce_holdem_Game",
                type: "TEXT",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxRebuys",
                table: "tce_holdem_Game",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MinBuyIn",
                table: "tce_holdem_Game",
                type: "TEXT",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Kind",
                table: "tce_holdem_BuyIn",
                type: "TEXT",
                maxLength: 32,
                nullable: false,
                defaultValue: "Join");

            migrationBuilder.UpdateData(
                table: "tce_holdem___TceSchema",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "EngineVersion", "SchemaVersion" },
                values: new object[] { "1.2.0", 3 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllowRebuy",
                table: "tce_holdem_Game");

            migrationBuilder.DropColumn(
                name: "AllowTopUp",
                table: "tce_holdem_Game");

            migrationBuilder.DropColumn(
                name: "MaxBuyIn",
                table: "tce_holdem_Game");

            migrationBuilder.DropColumn(
                name: "MaxRebuys",
                table: "tce_holdem_Game");

            migrationBuilder.DropColumn(
                name: "MinBuyIn",
                table: "tce_holdem_Game");

            migrationBuilder.DropColumn(
                name: "Kind",
                table: "tce_holdem_BuyIn");

            migrationBuilder.UpdateData(
                table: "tce_holdem___TceSchema",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "EngineVersion", "SchemaVersion" },
                values: new object[] { "1.1.0", 2 });
        }
    }
}
