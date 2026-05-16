using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogoVisualizer.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFixedLogoTechniqueAndColorCount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FixedLogoColorCount",
                table: "PrintZones",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FixedLogoTechnique",
                table: "PrintZones",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FixedLogoColorCount",
                table: "PrintZones");

            migrationBuilder.DropColumn(
                name: "FixedLogoTechnique",
                table: "PrintZones");
        }
    }
}
