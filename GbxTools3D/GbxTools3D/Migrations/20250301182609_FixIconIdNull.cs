using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GbxTools3D.Migrations
{
    /// <inheritdoc />
    public partial class FixIconIdNull : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BlockInfos_Icons_IconId",
                table: "BlockInfos");

            migrationBuilder.DropForeignKey(
                name: "FK_Collections_Icons_IconId",
                table: "Collections");

            migrationBuilder.AlterColumn<int>(
                name: "IconId",
                table: "Collections",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "IconId",
                table: "BlockInfos",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_BlockInfos_Icons_IconId",
                table: "BlockInfos",
                column: "IconId",
                principalTable: "Icons",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Collections_Icons_IconId",
                table: "Collections",
                column: "IconId",
                principalTable: "Icons",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BlockInfos_Icons_IconId",
                table: "BlockInfos");

            migrationBuilder.DropForeignKey(
                name: "FK_Collections_Icons_IconId",
                table: "Collections");

            migrationBuilder.AlterColumn<int>(
                name: "IconId",
                table: "Collections",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "IconId",
                table: "BlockInfos",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_BlockInfos_Icons_IconId",
                table: "BlockInfos",
                column: "IconId",
                principalTable: "Icons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Collections_Icons_IconId",
                table: "Collections",
                column: "IconId",
                principalTable: "Icons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
