using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GbxTools3D.Migrations
{
    /// <inheritdoc />
    public partial class AddMoreCollectionInfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DefaultZoneBlock",
                table: "Collections",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "SortIndex",
                table: "Collections",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SquareHeight",
                table: "Collections",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SquareSize",
                table: "Collections",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "VehicleAuthor",
                table: "Collections",
                type: "varchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "VehicleCollection",
                table: "Collections",
                type: "varchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "VehicleId",
                table: "Collections",
                type: "varchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DefaultZoneBlock",
                table: "Collections");

            migrationBuilder.DropColumn(
                name: "SortIndex",
                table: "Collections");

            migrationBuilder.DropColumn(
                name: "SquareHeight",
                table: "Collections");

            migrationBuilder.DropColumn(
                name: "SquareSize",
                table: "Collections");

            migrationBuilder.DropColumn(
                name: "VehicleAuthor",
                table: "Collections");

            migrationBuilder.DropColumn(
                name: "VehicleCollection",
                table: "Collections");

            migrationBuilder.DropColumn(
                name: "VehicleId",
                table: "Collections");
        }
    }
}
