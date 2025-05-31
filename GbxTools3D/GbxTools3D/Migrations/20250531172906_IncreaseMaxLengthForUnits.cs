using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GbxTools3D.Migrations
{
    /// <inheritdoc />
    public partial class IncreaseMaxLengthForUnits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<byte[]>(
                name: "GroundUnits",
                table: "BlockInfos",
                type: "varbinary(1536)",
                maxLength: 1536,
                nullable: false,
                oldClrType: typeof(byte[]),
                oldType: "varbinary(1024)",
                oldMaxLength: 1024);

            migrationBuilder.AlterColumn<byte[]>(
                name: "AirUnits",
                table: "BlockInfos",
                type: "varbinary(1536)",
                maxLength: 1536,
                nullable: false,
                oldClrType: typeof(byte[]),
                oldType: "varbinary(1024)",
                oldMaxLength: 1024);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<byte[]>(
                name: "GroundUnits",
                table: "BlockInfos",
                type: "varbinary(1024)",
                maxLength: 1024,
                nullable: false,
                oldClrType: typeof(byte[]),
                oldType: "varbinary(1536)",
                oldMaxLength: 1536);

            migrationBuilder.AlterColumn<byte[]>(
                name: "AirUnits",
                table: "BlockInfos",
                type: "varbinary(1024)",
                maxLength: 1024,
                nullable: false,
                oldClrType: typeof(byte[]),
                oldType: "varbinary(1536)",
                oldMaxLength: 1536);
        }
    }
}
