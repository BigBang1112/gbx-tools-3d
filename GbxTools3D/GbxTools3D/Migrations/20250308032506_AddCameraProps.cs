using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GbxTools3D.Migrations
{
    /// <inheritdoc />
    public partial class AddCameraProps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<float>(
                name: "CameraFar",
                table: "Vehicles",
                type: "float",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "CameraFov",
                table: "Vehicles",
                type: "float",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "CameraLookAtFactor",
                table: "Vehicles",
                type: "float",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "CameraUp",
                table: "Vehicles",
                type: "float",
                nullable: false,
                defaultValue: 0f);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CameraFar",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "CameraFov",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "CameraLookAtFactor",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "CameraUp",
                table: "Vehicles");
        }
    }
}
