using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TabletopCardEngine.PostgreSql.Migrations.Holdem
{
    /// <inheritdoc />
    internal partial class AddRaiseCap : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RaiseCap",
                schema: "tce_holdem",
                table: "Game",
                type: "integer",
                nullable: true);

            migrationBuilder.UpdateData(
                schema: "tce_holdem",
                table: "__TceSchema",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "EngineVersion", "SchemaVersion" },
                values: new object[] { "1.3.0", 5 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RaiseCap",
                schema: "tce_holdem",
                table: "Game");

            migrationBuilder.UpdateData(
                schema: "tce_holdem",
                table: "__TceSchema",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "EngineVersion", "SchemaVersion" },
                values: new object[] { "1.2.0", 4 });
        }
    }
}
