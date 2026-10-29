using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TabletopCardEngine.PostgreSql.Migrations.Holdem
{
    /// <inheritdoc />
    internal partial class AddGameTableFormat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TableFormat",
                schema: "tce_holdem",
                table: "Game",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "SixMax");

            migrationBuilder.UpdateData(
                schema: "tce_holdem",
                table: "__TceSchema",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "EngineVersion", "SchemaVersion" },
                values: new object[] { "1.1.0", 2 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TableFormat",
                schema: "tce_holdem",
                table: "Game");

            migrationBuilder.UpdateData(
                schema: "tce_holdem",
                table: "__TceSchema",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "EngineVersion", "SchemaVersion" },
                values: new object[] { "1.0.0", 1 });
        }
    }
}
