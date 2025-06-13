using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GbxTools3D.Migrations
{
    /// <inheritdoc />
    public partial class AddTerrainModifierCovered : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TerrainModifierCoveredId",
                table: "Decorations",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Decorations_TerrainModifierCoveredId",
                table: "Decorations",
                column: "TerrainModifierCoveredId");

            migrationBuilder.AddForeignKey(
                name: "FK_Decorations_TerrainModifiers_TerrainModifierCoveredId",
                table: "Decorations",
                column: "TerrainModifierCoveredId",
                principalTable: "TerrainModifiers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Decorations_TerrainModifiers_TerrainModifierCoveredId",
                table: "Decorations");

            migrationBuilder.DropIndex(
                name: "IX_Decorations_TerrainModifierCoveredId",
                table: "Decorations");

            migrationBuilder.DropColumn(
                name: "TerrainModifierCoveredId",
                table: "Decorations");
        }
    }
}
