using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GbxTools3D.Migrations
{
    /// <inheritdoc />
    public partial class IncreaseCampaignNameMaxLengthTo24 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Campaigns",
                type: "varchar(24)",
                maxLength: 24,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(16)",
                oldMaxLength: 16)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Campaigns",
                type: "varchar(16)",
                maxLength: 16,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(24)",
                oldMaxLength: 24)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }
    }
}
