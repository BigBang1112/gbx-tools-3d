using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GbxTools3D.Migrations
{
    /// <inheritdoc />
    public partial class AddIconSmall : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "IconSmallId",
                table: "Collections",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Collections_IconSmallId",
                table: "Collections",
                column: "IconSmallId");

            migrationBuilder.AddForeignKey(
                name: "FK_Collections_Icons_IconSmallId",
                table: "Collections",
                column: "IconSmallId",
                principalTable: "Icons",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Collections_Icons_IconSmallId",
                table: "Collections");

            migrationBuilder.DropIndex(
                name: "IX_Collections_IconSmallId",
                table: "Collections");

            migrationBuilder.DropColumn(
                name: "IconSmallId",
                table: "Collections");
        }
    }
}
