using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TabletopCardEngine.SqlServer.Migrations.Holdem
{
    /// <inheritdoc />
    internal partial class AddBuyInOptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AllowRebuy",
                schema: "tce_holdem",
                table: "Game",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AllowTopUp",
                schema: "tce_holdem",
                table: "Game",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "MaxBuyIn",
                schema: "tce_holdem",
                table: "Game",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxRebuys",
                schema: "tce_holdem",
                table: "Game",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MinBuyIn",
                schema: "tce_holdem",
                table: "Game",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Kind",
                schema: "tce_holdem",
                table: "BuyIn",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Join");

            migrationBuilder.UpdateData(
                schema: "tce_holdem",
                table: "__TceSchema",
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
                schema: "tce_holdem",
                table: "Game");

            migrationBuilder.DropColumn(
                name: "AllowTopUp",
                schema: "tce_holdem",
                table: "Game");

            migrationBuilder.DropColumn(
                name: "MaxBuyIn",
                schema: "tce_holdem",
                table: "Game");

            migrationBuilder.DropColumn(
                name: "MaxRebuys",
                schema: "tce_holdem",
                table: "Game");

            migrationBuilder.DropColumn(
                name: "MinBuyIn",
                schema: "tce_holdem",
                table: "Game");

            migrationBuilder.DropColumn(
                name: "Kind",
                schema: "tce_holdem",
                table: "BuyIn");

            migrationBuilder.UpdateData(
                schema: "tce_holdem",
                table: "__TceSchema",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "EngineVersion", "SchemaVersion" },
                values: new object[] { "1.1.0", 2 });
        }
    }
}
