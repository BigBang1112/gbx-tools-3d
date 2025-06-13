using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GbxTools3D.Migrations
{
    /// <inheritdoc />
    public partial class AddTerrainModifierToBlockInfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TerrainModifierId",
                table: "BlockInfos",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BlockInfos_TerrainModifierId",
                table: "BlockInfos",
                column: "TerrainModifierId");

            migrationBuilder.AddForeignKey(
                name: "FK_BlockInfos_TerrainModifiers_TerrainModifierId",
                table: "BlockInfos",
                column: "TerrainModifierId",
                principalTable: "TerrainModifiers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BlockInfos_TerrainModifiers_TerrainModifierId",
                table: "BlockInfos");

            migrationBuilder.DropIndex(
                name: "IX_BlockInfos_TerrainModifierId",
                table: "BlockInfos");

            migrationBuilder.DropColumn(
                name: "TerrainModifierId",
                table: "BlockInfos");
        }
    }
}
