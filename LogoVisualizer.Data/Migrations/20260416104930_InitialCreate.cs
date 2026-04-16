using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace LogoVisualizer.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PrintTechniques",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrintTechniques", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ImagePath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ImageWidth = table.Column<int>(type: "int", nullable: false),
                    ImageHeight = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PrintZones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    X = table.Column<int>(type: "int", nullable: false),
                    Y = table.Column<int>(type: "int", nullable: false),
                    Width = table.Column<int>(type: "int", nullable: false),
                    Height = table.Column<int>(type: "int", nullable: false),
                    MaxPhysicalWidthMm = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MaxPhysicalHeightMm = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MaxColors = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrintZones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PrintZones_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PrintZoneTechniques",
                columns: table => new
                {
                    PrintZoneId = table.Column<int>(type: "int", nullable: false),
                    PrintTechniqueId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrintZoneTechniques", x => new { x.PrintZoneId, x.PrintTechniqueId });
                    table.ForeignKey(
                        name: "FK_PrintZoneTechniques_PrintTechniques_PrintTechniqueId",
                        column: x => x.PrintTechniqueId,
                        principalTable: "PrintTechniques",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PrintZoneTechniques_PrintZones_PrintZoneId",
                        column: x => x.PrintZoneId,
                        principalTable: "PrintZones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "PrintTechniques",
                columns: new[] { "Id", "Description", "Name" },
                values: new object[,]
                {
                    { 1, "Silkscreen/serigrafi-tryk", "Screen Print" },
                    { 2, "Broderi", "Embroidery" },
                    { 3, "Sublimationstryk (kræver lyst syntetisk stof)", "Sublimation" },
                    { 4, "Laser- eller mekanisk gravering", "Engraving" },
                    { 5, "Direct-to-garment tryk", "DTG" },
                    { 6, "Tampontryk — velegnet til små flader", "Pad Print" },
                    { 7, "Digitalt tryk / inkjet", "Digital Print" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_PrintTechniques_Name",
                table: "PrintTechniques",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PrintZones_ProductId",
                table: "PrintZones",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_PrintZoneTechniques_PrintTechniqueId",
                table: "PrintZoneTechniques",
                column: "PrintTechniqueId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PrintZoneTechniques");

            migrationBuilder.DropTable(
                name: "PrintTechniques");

            migrationBuilder.DropTable(
                name: "PrintZones");

            migrationBuilder.DropTable(
                name: "Products");
        }
    }
}
