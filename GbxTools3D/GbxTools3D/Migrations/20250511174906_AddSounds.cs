using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GbxTools3D.Migrations
{
    /// <inheritdoc />
    public partial class AddSounds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SoundId",
                table: "ObjectLinks",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Sounds",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    GameVersion = table.Column<int>(type: "int", nullable: false),
                    Hash = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Data = table.Column<byte[]>(type: "longblob", maxLength: 16777215, nullable: false),
                    Path = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Type = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    AudioPath = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sounds", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ObjectLinks_SoundId",
                table: "ObjectLinks",
                column: "SoundId");

            migrationBuilder.CreateIndex(
                name: "IX_Sounds_Hash",
                table: "Sounds",
                column: "Hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sounds_Path_GameVersion",
                table: "Sounds",
                columns: new[] { "Path", "GameVersion" });

            migrationBuilder.AddForeignKey(
                name: "FK_ObjectLinks_Sounds_SoundId",
                table: "ObjectLinks",
                column: "SoundId",
                principalTable: "Sounds",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ObjectLinks_Sounds_SoundId",
                table: "ObjectLinks");

            migrationBuilder.DropTable(
                name: "Sounds");

            migrationBuilder.DropIndex(
                name: "IX_ObjectLinks_SoundId",
                table: "ObjectLinks");

            migrationBuilder.DropColumn(
                name: "SoundId",
                table: "ObjectLinks");
        }
    }
}
