using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogoVisualizer.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenameToSlugTechniques : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "PrintTechniques",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.UpdateData(
                table: "PrintTechniques",
                keyColumn: "Id",
                keyValue: 1,
                column: "Name",
                value: "screen_print");

            migrationBuilder.UpdateData(
                table: "PrintTechniques",
                keyColumn: "Id",
                keyValue: 2,
                column: "Name",
                value: "embroidery");

            migrationBuilder.UpdateData(
                table: "PrintTechniques",
                keyColumn: "Id",
                keyValue: 3,
                column: "Name",
                value: "sublimation");

            migrationBuilder.UpdateData(
                table: "PrintTechniques",
                keyColumn: "Id",
                keyValue: 4,
                column: "Name",
                value: "engraving");

            migrationBuilder.UpdateData(
                table: "PrintTechniques",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "Description", "Name" },
                values: new object[] { "Digitalt tryk / inkjet", "digital_print" });

            migrationBuilder.UpdateData(
                table: "PrintTechniques",
                keyColumn: "Id",
                keyValue: 6,
                column: "Name",
                value: "pad_print");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "PrintTechniques",
                keyColumn: "Id",
                keyValue: 1,
                column: "Name",
                value: "Screen Print");

            migrationBuilder.UpdateData(
                table: "PrintTechniques",
                keyColumn: "Id",
                keyValue: 2,
                column: "Name",
                value: "Embroidery");

            migrationBuilder.UpdateData(
                table: "PrintTechniques",
                keyColumn: "Id",
                keyValue: 3,
                column: "Name",
                value: "Sublimation");

            migrationBuilder.UpdateData(
                table: "PrintTechniques",
                keyColumn: "Id",
                keyValue: 4,
                column: "Name",
                value: "Engraving");

            migrationBuilder.UpdateData(
                table: "PrintTechniques",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "Description", "Name" },
                values: new object[] { "Direct-to-garment tryk", "DTG" });

            migrationBuilder.UpdateData(
                table: "PrintTechniques",
                keyColumn: "Id",
                keyValue: 6,
                column: "Name",
                value: "Pad Print");

            migrationBuilder.InsertData(
                table: "PrintTechniques",
                columns: new[] { "Id", "Description", "Name" },
                values: new object[] { 7, "Digitalt tryk / inkjet", "Digital Print" });
        }
    }
}
