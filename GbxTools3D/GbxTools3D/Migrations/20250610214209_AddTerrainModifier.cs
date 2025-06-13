using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GbxTools3D.Migrations
{
    /// <inheritdoc />
    public partial class AddTerrainModifier : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ModifierId",
                table: "Materials",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModifierOfMaterial",
                table: "Materials",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "TerrainModifiers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CollectionId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TerrainModifiers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TerrainModifiers_Collections_CollectionId",
                        column: x => x.CollectionId,
                        principalTable: "Collections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Materials_ModifierId",
                table: "Materials",
                column: "ModifierId");

            migrationBuilder.CreateIndex(
                name: "IX_TerrainModifiers_CollectionId",
                table: "TerrainModifiers",
                column: "CollectionId");

            migrationBuilder.CreateIndex(
                name: "IX_TerrainModifiers_Name",
                table: "TerrainModifiers",
                column: "Name");

            migrationBuilder.AddForeignKey(
                name: "FK_Materials_TerrainModifiers_ModifierId",
                table: "Materials",
                column: "ModifierId",
                principalTable: "TerrainModifiers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Materials_TerrainModifiers_ModifierId",
                table: "Materials");

            migrationBuilder.DropTable(
                name: "TerrainModifiers");

            migrationBuilder.DropIndex(
                name: "IX_Materials_ModifierId",
                table: "Materials");

            migrationBuilder.DropColumn(
                name: "ModifierId",
                table: "Materials");

            migrationBuilder.DropColumn(
                name: "ModifierOfMaterial",
                table: "Materials");
        }
    }
}
