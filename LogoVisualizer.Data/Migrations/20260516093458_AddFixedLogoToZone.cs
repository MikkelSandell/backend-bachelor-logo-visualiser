using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogoVisualizer.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFixedLogoToZone : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FixedLogoFileId",
                table: "PrintZones",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FixedLogoHeight",
                table: "PrintZones",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FixedLogoUrl",
                table: "PrintZones",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FixedLogoWidth",
                table: "PrintZones",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FixedLogoX",
                table: "PrintZones",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FixedLogoY",
                table: "PrintZones",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FixedLogoFileId",
                table: "PrintZones");

            migrationBuilder.DropColumn(
                name: "FixedLogoHeight",
                table: "PrintZones");

            migrationBuilder.DropColumn(
                name: "FixedLogoUrl",
                table: "PrintZones");

            migrationBuilder.DropColumn(
                name: "FixedLogoWidth",
                table: "PrintZones");

            migrationBuilder.DropColumn(
                name: "FixedLogoX",
                table: "PrintZones");

            migrationBuilder.DropColumn(
                name: "FixedLogoY",
                table: "PrintZones");
        }
    }
}
