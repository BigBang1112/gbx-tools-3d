using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GbxTools3D.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Collections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DisplayName = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    GameVersion = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Collections", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Meshes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Hash = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Data = table.Column<byte[]>(type: "longblob", maxLength: 16777215, nullable: false),
                    DataLQ = table.Column<byte[]>(type: "longblob", maxLength: 16777215, nullable: true),
                    DataVLQ = table.Column<byte[]>(type: "longblob", maxLength: 16777215, nullable: true),
                    DataSurf = table.Column<byte[]>(type: "longblob", maxLength: 16777215, nullable: true),
                    Path = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Meshes", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "BlockInfos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CollectionId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AirUnits = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    GroundUnits = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Height = table.Column<byte>(type: "tinyint unsigned", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlockInfos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BlockInfos_Collections_CollectionId",
                        column: x => x.CollectionId,
                        principalTable: "Collections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "BlockVariants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    BlockInfoId = table.Column<int>(type: "int", nullable: false),
                    Ground = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Variant = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    SubVariant = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    MeshId = table.Column<int>(type: "int", nullable: false),
                    Path = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlockVariants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BlockVariants_BlockInfos_BlockInfoId",
                        column: x => x.BlockInfoId,
                        principalTable: "BlockInfos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BlockVariants_Meshes_MeshId",
                        column: x => x.MeshId,
                        principalTable: "Meshes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ObjectLinks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Index = table.Column<int>(type: "int", nullable: false),
                    VariantId = table.Column<int>(type: "int", nullable: false),
                    MeshId = table.Column<int>(type: "int", nullable: false),
                    Path = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    XX = table.Column<float>(type: "float", nullable: false),
                    XY = table.Column<float>(type: "float", nullable: false),
                    XZ = table.Column<float>(type: "float", nullable: false),
                    YX = table.Column<float>(type: "float", nullable: false),
                    YY = table.Column<float>(type: "float", nullable: false),
                    YZ = table.Column<float>(type: "float", nullable: false),
                    ZX = table.Column<float>(type: "float", nullable: false),
                    ZY = table.Column<float>(type: "float", nullable: false),
                    ZZ = table.Column<float>(type: "float", nullable: false),
                    TX = table.Column<float>(type: "float", nullable: false),
                    TY = table.Column<float>(type: "float", nullable: false),
                    TZ = table.Column<float>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ObjectLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ObjectLinks_BlockVariants_VariantId",
                        column: x => x.VariantId,
                        principalTable: "BlockVariants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ObjectLinks_Meshes_MeshId",
                        column: x => x.MeshId,
                        principalTable: "Meshes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_BlockInfos_CollectionId",
                table: "BlockInfos",
                column: "CollectionId");

            migrationBuilder.CreateIndex(
                name: "IX_BlockInfos_Name",
                table: "BlockInfos",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_BlockVariants_BlockInfoId",
                table: "BlockVariants",
                column: "BlockInfoId");

            migrationBuilder.CreateIndex(
                name: "IX_BlockVariants_MeshId",
                table: "BlockVariants",
                column: "MeshId");

            migrationBuilder.CreateIndex(
                name: "IX_Meshes_Hash",
                table: "Meshes",
                column: "Hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ObjectLinks_MeshId",
                table: "ObjectLinks",
                column: "MeshId");

            migrationBuilder.CreateIndex(
                name: "IX_ObjectLinks_VariantId",
                table: "ObjectLinks",
                column: "VariantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ObjectLinks");

            migrationBuilder.DropTable(
                name: "BlockVariants");

            migrationBuilder.DropTable(
                name: "BlockInfos");

            migrationBuilder.DropTable(
                name: "Meshes");

            migrationBuilder.DropTable(
                name: "Collections");
        }
    }
}
