using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GbxTools3D.Migrations
{
    /// <inheritdoc />
    public partial class AddBlockHelpers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HasAirHelper",
                table: "BlockInfos",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasConstructionModeHelper",
                table: "BlockInfos",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasGroundHelper",
                table: "BlockInfos",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HasAirHelper",
                table: "BlockInfos");

            migrationBuilder.DropColumn(
                name: "HasConstructionModeHelper",
                table: "BlockInfos");

            migrationBuilder.DropColumn(
                name: "HasGroundHelper",
                table: "BlockInfos");
        }
    }
}
