using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GbxTools3D.Migrations
{
    /// <inheritdoc />
    public partial class OptimizeUnits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<byte[]>(
                name: "GroundUnits",
                table: "BlockInfos",
                type: "varbinary(1024)",
                maxLength: 1024,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(6144)",
                oldMaxLength: 6144)
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<byte[]>(
                name: "AirUnits",
                table: "BlockInfos",
                type: "varbinary(1024)",
                maxLength: 1024,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(6144)",
                oldMaxLength: 6144)
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "GroundUnits",
                table: "BlockInfos",
                type: "varchar(6144)",
                maxLength: 6144,
                nullable: false,
                oldClrType: typeof(byte[]),
                oldType: "varbinary(1024)",
                oldMaxLength: 1024)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "AirUnits",
                table: "BlockInfos",
                type: "varchar(6144)",
                maxLength: 6144,
                nullable: false,
                oldClrType: typeof(byte[]),
                oldType: "varbinary(1024)",
                oldMaxLength: 1024)
                .Annotation("MySql:CharSet", "utf8mb4");
        }
    }
}
