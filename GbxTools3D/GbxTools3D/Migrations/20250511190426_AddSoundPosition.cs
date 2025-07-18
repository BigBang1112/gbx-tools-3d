using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GbxTools3D.Migrations
{
    /// <inheritdoc />
    public partial class AddSoundPosition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<float>(
                name: "TX",
                table: "Sounds",
                type: "float",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "TY",
                table: "Sounds",
                type: "float",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "TZ",
                table: "Sounds",
                type: "float",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "XX",
                table: "Sounds",
                type: "float",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "XY",
                table: "Sounds",
                type: "float",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "XZ",
                table: "Sounds",
                type: "float",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "YX",
                table: "Sounds",
                type: "float",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "YY",
                table: "Sounds",
                type: "float",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "YZ",
                table: "Sounds",
                type: "float",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "ZX",
                table: "Sounds",
                type: "float",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "ZY",
                table: "Sounds",
                type: "float",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "ZZ",
                table: "Sounds",
                type: "float",
                nullable: false,
                defaultValue: 0f);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TX",
                table: "Sounds");

            migrationBuilder.DropColumn(
                name: "TY",
                table: "Sounds");

            migrationBuilder.DropColumn(
                name: "TZ",
                table: "Sounds");

            migrationBuilder.DropColumn(
                name: "XX",
                table: "Sounds");

            migrationBuilder.DropColumn(
                name: "XY",
                table: "Sounds");

            migrationBuilder.DropColumn(
                name: "XZ",
                table: "Sounds");

            migrationBuilder.DropColumn(
                name: "YX",
                table: "Sounds");

            migrationBuilder.DropColumn(
                name: "YY",
                table: "Sounds");

            migrationBuilder.DropColumn(
                name: "YZ",
                table: "Sounds");

            migrationBuilder.DropColumn(
                name: "ZX",
                table: "Sounds");

            migrationBuilder.DropColumn(
                name: "ZY",
                table: "Sounds");

            migrationBuilder.DropColumn(
                name: "ZZ",
                table: "Sounds");
        }
    }
}
