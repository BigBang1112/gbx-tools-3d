using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GbxTools3D.Migrations
{
    /// <inheritdoc />
    public partial class MoveCollectionToDecoSize : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Decorations_Collections_CollectionId",
                table: "Decorations");

            migrationBuilder.DropIndex(
                name: "IX_Decorations_CollectionId",
                table: "Decorations");

            migrationBuilder.DropColumn(
                name: "CollectionId",
                table: "Decorations");

            migrationBuilder.AddColumn<int>(
                name: "CollectionId",
                table: "DecorationSizes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_DecorationSizes_CollectionId",
                table: "DecorationSizes",
                column: "CollectionId");

            migrationBuilder.AddForeignKey(
                name: "FK_DecorationSizes_Collections_CollectionId",
                table: "DecorationSizes",
                column: "CollectionId",
                principalTable: "Collections",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DecorationSizes_Collections_CollectionId",
                table: "DecorationSizes");

            migrationBuilder.DropIndex(
                name: "IX_DecorationSizes_CollectionId",
                table: "DecorationSizes");

            migrationBuilder.DropColumn(
                name: "CollectionId",
                table: "DecorationSizes");

            migrationBuilder.AddColumn<int>(
                name: "CollectionId",
                table: "Decorations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Decorations_CollectionId",
                table: "Decorations",
                column: "CollectionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Decorations_Collections_CollectionId",
                table: "Decorations",
                column: "CollectionId",
                principalTable: "Collections",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
