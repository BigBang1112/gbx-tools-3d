using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GbxTools3D.Migrations
{
    /// <inheritdoc />
    public partial class IncreaseBlockUnitLength : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "GroundUnits",
                table: "BlockInfos",
                type: "varchar(6144)",
                maxLength: 6144,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(4096)",
                oldMaxLength: 4096)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "AirUnits",
                table: "BlockInfos",
                type: "varchar(6144)",
                maxLength: 6144,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(4096)",
                oldMaxLength: 4096)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "GroundUnits",
                table: "BlockInfos",
                type: "varchar(4096)",
                maxLength: 4096,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(6144)",
                oldMaxLength: 6144)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "AirUnits",
                table: "BlockInfos",
                type: "varchar(4096)",
                maxLength: 4096,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(6144)",
                oldMaxLength: 6144)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }
    }
}
