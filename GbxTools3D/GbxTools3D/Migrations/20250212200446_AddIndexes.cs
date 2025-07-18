using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GbxTools3D.Migrations
{
    /// <inheritdoc />
    public partial class AddIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Materials_GameVersion",
                table: "Materials");

            migrationBuilder.CreateIndex(
                name: "IX_Textures_Hash",
                table: "Textures",
                column: "Hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Textures_Path_GameVersion",
                table: "Textures",
                columns: new[] { "Path", "GameVersion" });

            migrationBuilder.CreateIndex(
                name: "IX_Materials_GameVersion_Name",
                table: "Materials",
                columns: new[] { "GameVersion", "Name" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Textures_Hash",
                table: "Textures");

            migrationBuilder.DropIndex(
                name: "IX_Textures_Path_GameVersion",
                table: "Textures");

            migrationBuilder.DropIndex(
                name: "IX_Materials_GameVersion_Name",
                table: "Materials");

            migrationBuilder.CreateIndex(
                name: "IX_Materials_GameVersion",
                table: "Materials",
                column: "GameVersion");
        }
    }
}
