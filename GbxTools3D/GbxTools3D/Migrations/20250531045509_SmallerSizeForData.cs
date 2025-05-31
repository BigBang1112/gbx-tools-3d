using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GbxTools3D.Migrations
{
    /// <inheritdoc />
    public partial class SmallerSizeForData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<byte[]>(
                name: "Data",
                table: "Textures",
                type: "mediumblob",
                nullable: false,
                oldClrType: typeof(byte[]),
                oldType: "longblob",
                oldMaxLength: 16777215);

            migrationBuilder.AlterColumn<byte[]>(
                name: "DataVLQ",
                table: "Meshes",
                type: "mediumblob",
                nullable: true,
                oldClrType: typeof(byte[]),
                oldType: "longblob",
                oldMaxLength: 16777215,
                oldNullable: true);

            migrationBuilder.AlterColumn<byte[]>(
                name: "DataSurf",
                table: "Meshes",
                type: "mediumblob",
                nullable: true,
                oldClrType: typeof(byte[]),
                oldType: "longblob",
                oldMaxLength: 16777215,
                oldNullable: true);

            migrationBuilder.AlterColumn<byte[]>(
                name: "DataLQ",
                table: "Meshes",
                type: "mediumblob",
                nullable: true,
                oldClrType: typeof(byte[]),
                oldType: "longblob",
                oldMaxLength: 16777215,
                oldNullable: true);

            migrationBuilder.AlterColumn<byte[]>(
                name: "Data",
                table: "Meshes",
                type: "mediumblob",
                nullable: false,
                oldClrType: typeof(byte[]),
                oldType: "longblob",
                oldMaxLength: 16777215);

            migrationBuilder.AlterColumn<byte[]>(
                name: "Data",
                table: "Icons",
                type: "blob",
                nullable: true,
                oldClrType: typeof(byte[]),
                oldType: "longblob",
                oldMaxLength: 16777215,
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<byte[]>(
                name: "Data",
                table: "Textures",
                type: "longblob",
                maxLength: 16777215,
                nullable: false,
                oldClrType: typeof(byte[]),
                oldType: "mediumblob");

            migrationBuilder.AlterColumn<byte[]>(
                name: "DataVLQ",
                table: "Meshes",
                type: "longblob",
                maxLength: 16777215,
                nullable: true,
                oldClrType: typeof(byte[]),
                oldType: "mediumblob",
                oldNullable: true);

            migrationBuilder.AlterColumn<byte[]>(
                name: "DataSurf",
                table: "Meshes",
                type: "longblob",
                maxLength: 16777215,
                nullable: true,
                oldClrType: typeof(byte[]),
                oldType: "mediumblob",
                oldNullable: true);

            migrationBuilder.AlterColumn<byte[]>(
                name: "DataLQ",
                table: "Meshes",
                type: "longblob",
                maxLength: 16777215,
                nullable: true,
                oldClrType: typeof(byte[]),
                oldType: "mediumblob",
                oldNullable: true);

            migrationBuilder.AlterColumn<byte[]>(
                name: "Data",
                table: "Meshes",
                type: "longblob",
                maxLength: 16777215,
                nullable: false,
                oldClrType: typeof(byte[]),
                oldType: "mediumblob");

            migrationBuilder.AlterColumn<byte[]>(
                name: "Data",
                table: "Icons",
                type: "longblob",
                maxLength: 16777215,
                nullable: true,
                oldClrType: typeof(byte[]),
                oldType: "blob",
                oldNullable: true);
        }
    }
}
