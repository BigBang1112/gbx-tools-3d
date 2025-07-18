using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GbxTools3D.Migrations
{
    /// <inheritdoc />
    public partial class AddShader : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ShaderId",
                table: "Materials",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Materials_GameVersion",
                table: "Materials",
                column: "GameVersion");

            migrationBuilder.CreateIndex(
                name: "IX_Materials_ShaderId",
                table: "Materials",
                column: "ShaderId");

            migrationBuilder.AddForeignKey(
                name: "FK_Materials_Materials_ShaderId",
                table: "Materials",
                column: "ShaderId",
                principalTable: "Materials",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Materials_Materials_ShaderId",
                table: "Materials");

            migrationBuilder.DropIndex(
                name: "IX_Materials_GameVersion",
                table: "Materials");

            migrationBuilder.DropIndex(
                name: "IX_Materials_ShaderId",
                table: "Materials");

            migrationBuilder.DropColumn(
                name: "ShaderId",
                table: "Materials");
        }
    }
}
