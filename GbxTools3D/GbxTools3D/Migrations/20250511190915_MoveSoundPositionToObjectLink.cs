using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GbxTools3D.Migrations
{
    /// <inheritdoc />
    public partial class MoveSoundPositionToObjectLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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

            migrationBuilder.AddColumn<float>(
                name: "SoundTX",
                table: "ObjectLinks",
                type: "float",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "SoundTY",
                table: "ObjectLinks",
                type: "float",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "SoundTZ",
                table: "ObjectLinks",
                type: "float",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "SoundXX",
                table: "ObjectLinks",
                type: "float",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "SoundXY",
                table: "ObjectLinks",
                type: "float",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "SoundXZ",
                table: "ObjectLinks",
                type: "float",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "SoundYX",
                table: "ObjectLinks",
                type: "float",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "SoundYY",
                table: "ObjectLinks",
                type: "float",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "SoundYZ",
                table: "ObjectLinks",
                type: "float",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "SoundZX",
                table: "ObjectLinks",
                type: "float",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "SoundZY",
                table: "ObjectLinks",
                type: "float",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "SoundZZ",
                table: "ObjectLinks",
                type: "float",
                nullable: false,
                defaultValue: 0f);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SoundTX",
                table: "ObjectLinks");

            migrationBuilder.DropColumn(
                name: "SoundTY",
                table: "ObjectLinks");

            migrationBuilder.DropColumn(
                name: "SoundTZ",
                table: "ObjectLinks");

            migrationBuilder.DropColumn(
                name: "SoundXX",
                table: "ObjectLinks");

            migrationBuilder.DropColumn(
                name: "SoundXY",
                table: "ObjectLinks");

            migrationBuilder.DropColumn(
                name: "SoundXZ",
                table: "ObjectLinks");

            migrationBuilder.DropColumn(
                name: "SoundYX",
                table: "ObjectLinks");

            migrationBuilder.DropColumn(
                name: "SoundYY",
                table: "ObjectLinks");

            migrationBuilder.DropColumn(
                name: "SoundYZ",
                table: "ObjectLinks");

            migrationBuilder.DropColumn(
                name: "SoundZX",
                table: "ObjectLinks");

            migrationBuilder.DropColumn(
                name: "SoundZY",
                table: "ObjectLinks");

            migrationBuilder.DropColumn(
                name: "SoundZZ",
                table: "ObjectLinks");

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
    }
}
