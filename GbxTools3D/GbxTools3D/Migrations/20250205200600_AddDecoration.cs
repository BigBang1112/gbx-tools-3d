using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GbxTools3D.Migrations
{
    /// <inheritdoc />
    public partial class AddDecoration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DecorationSizes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    SizeX = table.Column<int>(type: "int", nullable: false),
                    SizeY = table.Column<int>(type: "int", nullable: false),
                    SizeZ = table.Column<int>(type: "int", nullable: false),
                    BaseHeight = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DecorationSizes", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Decorations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CollectionId = table.Column<int>(type: "int", nullable: false),
                    DecorationSizeId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Decorations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Decorations_Collections_CollectionId",
                        column: x => x.CollectionId,
                        principalTable: "Collections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Decorations_DecorationSizes_DecorationSizeId",
                        column: x => x.DecorationSizeId,
                        principalTable: "DecorationSizes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Decorations_CollectionId",
                table: "Decorations",
                column: "CollectionId");

            migrationBuilder.CreateIndex(
                name: "IX_Decorations_DecorationSizeId",
                table: "Decorations",
                column: "DecorationSizeId");

            migrationBuilder.CreateIndex(
                name: "IX_DecorationSizes_SizeX_SizeY_SizeZ",
                table: "DecorationSizes",
                columns: new[] { "SizeX", "SizeY", "SizeZ" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Decorations");

            migrationBuilder.DropTable(
                name: "DecorationSizes");
        }
    }
}
