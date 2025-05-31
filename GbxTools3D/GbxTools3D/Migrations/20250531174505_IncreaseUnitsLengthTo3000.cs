using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GbxTools3D.Migrations
{
    /// <inheritdoc />
    public partial class IncreaseUnitsLengthTo3000 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<byte[]>(
                name: "GroundUnits",
                table: "BlockInfos",
                type: "varbinary(3000)",
                maxLength: 3000,
                nullable: false,
                oldClrType: typeof(byte[]),
                oldType: "varbinary(1536)",
                oldMaxLength: 1536);

            migrationBuilder.AlterColumn<byte[]>(
                name: "AirUnits",
                table: "BlockInfos",
                type: "varbinary(3000)",
                maxLength: 3000,
                nullable: false,
                oldClrType: typeof(byte[]),
                oldType: "varbinary(1536)",
                oldMaxLength: 1536);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<byte[]>(
                name: "GroundUnits",
                table: "BlockInfos",
                type: "varbinary(1536)",
                maxLength: 1536,
                nullable: false,
                oldClrType: typeof(byte[]),
                oldType: "varbinary(3000)",
                oldMaxLength: 3000);

            migrationBuilder.AlterColumn<byte[]>(
                name: "AirUnits",
                table: "BlockInfos",
                type: "varbinary(1536)",
                maxLength: 1536,
                nullable: false,
                oldClrType: typeof(byte[]),
                oldType: "varbinary(3000)",
                oldMaxLength: 3000);
        }
    }
}
