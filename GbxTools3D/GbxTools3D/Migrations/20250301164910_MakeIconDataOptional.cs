using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GbxTools3D.Migrations
{
    /// <inheritdoc />
    public partial class MakeIconDataOptional : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<byte[]>(
                name: "Data",
                table: "Icons",
                type: "longblob",
                maxLength: 16777215,
                nullable: true,
                oldClrType: typeof(byte[]),
                oldType: "longblob",
                oldMaxLength: 16777215);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<byte[]>(
                name: "Data",
                table: "Icons",
                type: "longblob",
                maxLength: 16777215,
                nullable: false,
                defaultValue: new byte[0],
                oldClrType: typeof(byte[]),
                oldType: "longblob",
                oldMaxLength: 16777215,
                oldNullable: true);
        }
    }
}
