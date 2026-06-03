using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogoVisualizer.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenameBumArtikelColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "FixedLogoY",
                table: "PrintZones",
                newName: "BumArtikelY");

            migrationBuilder.RenameColumn(
                name: "FixedLogoX",
                table: "PrintZones",
                newName: "BumArtikelX");

            migrationBuilder.RenameColumn(
                name: "FixedLogoWidth",
                table: "PrintZones",
                newName: "BumArtikelWidth");

            migrationBuilder.RenameColumn(
                name: "FixedLogoUrl",
                table: "PrintZones",
                newName: "BumArtikelUrl");

            migrationBuilder.RenameColumn(
                name: "FixedLogoTechnique",
                table: "PrintZones",
                newName: "BumArtikelTechnique");

            migrationBuilder.RenameColumn(
                name: "FixedLogoHeight",
                table: "PrintZones",
                newName: "BumArtikelHeight");

            migrationBuilder.RenameColumn(
                name: "FixedLogoFileId",
                table: "PrintZones",
                newName: "BumArtikelFileId");

            migrationBuilder.RenameColumn(
                name: "FixedLogoColorCount",
                table: "PrintZones",
                newName: "BumArtikelColorCount");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "BumArtikelY",
                table: "PrintZones",
                newName: "FixedLogoY");

            migrationBuilder.RenameColumn(
                name: "BumArtikelX",
                table: "PrintZones",
                newName: "FixedLogoX");

            migrationBuilder.RenameColumn(
                name: "BumArtikelWidth",
                table: "PrintZones",
                newName: "FixedLogoWidth");

            migrationBuilder.RenameColumn(
                name: "BumArtikelUrl",
                table: "PrintZones",
                newName: "FixedLogoUrl");

            migrationBuilder.RenameColumn(
                name: "BumArtikelTechnique",
                table: "PrintZones",
                newName: "FixedLogoTechnique");

            migrationBuilder.RenameColumn(
                name: "BumArtikelHeight",
                table: "PrintZones",
                newName: "FixedLogoHeight");

            migrationBuilder.RenameColumn(
                name: "BumArtikelFileId",
                table: "PrintZones",
                newName: "FixedLogoFileId");

            migrationBuilder.RenameColumn(
                name: "BumArtikelColorCount",
                table: "PrintZones",
                newName: "FixedLogoColorCount");
        }
    }
}
