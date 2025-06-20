using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GbxTools3D.Migrations
{
    /// <inheritdoc />
    public partial class AddSceneName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SceneName",
                table: "DecorationSizes",
                type: "varchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SceneName",
                table: "DecorationSizes");
        }
    }
}
