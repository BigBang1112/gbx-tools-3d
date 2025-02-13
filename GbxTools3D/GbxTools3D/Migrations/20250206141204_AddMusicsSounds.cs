using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GbxTools3D.Migrations
{
    /// <inheritdoc />
    public partial class AddMusicsSounds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Musics",
                table: "Decorations",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Sounds",
                table: "Decorations",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Musics",
                table: "Decorations");

            migrationBuilder.DropColumn(
                name: "Sounds",
                table: "Decorations");
        }
    }
}
