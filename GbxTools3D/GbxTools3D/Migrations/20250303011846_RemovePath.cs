using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GbxTools3D.Migrations
{
    /// <inheritdoc />
    public partial class RemovePath : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Materials_GameVersion_Name",
                table: "Materials");

            migrationBuilder.DropColumn(
                name: "Path",
                table: "Materials");

            migrationBuilder.CreateIndex(
                name: "IX_Materials_GameVersion",
                table: "Materials",
                column: "GameVersion");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Materials_GameVersion",
                table: "Materials");

            migrationBuilder.AddColumn<string>(
                name: "Path",
                table: "Materials",
                type: "varchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Materials_GameVersion_Name",
                table: "Materials",
                columns: new[] { "GameVersion", "Name" });
        }
    }
}
