using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GbxTools3D.Migrations
{
    /// <inheritdoc />
    public partial class AddComplexProperties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ZZ",
                table: "ObjectLinks",
                newName: "Loc_ZZ");

            migrationBuilder.RenameColumn(
                name: "ZY",
                table: "ObjectLinks",
                newName: "Loc_ZY");

            migrationBuilder.RenameColumn(
                name: "ZX",
                table: "ObjectLinks",
                newName: "Loc_ZX");

            migrationBuilder.RenameColumn(
                name: "YZ",
                table: "ObjectLinks",
                newName: "Loc_YZ");

            migrationBuilder.RenameColumn(
                name: "YY",
                table: "ObjectLinks",
                newName: "Loc_YY");

            migrationBuilder.RenameColumn(
                name: "YX",
                table: "ObjectLinks",
                newName: "Loc_YX");

            migrationBuilder.RenameColumn(
                name: "XZ",
                table: "ObjectLinks",
                newName: "Loc_XZ");

            migrationBuilder.RenameColumn(
                name: "XY",
                table: "ObjectLinks",
                newName: "Loc_XY");

            migrationBuilder.RenameColumn(
                name: "XX",
                table: "ObjectLinks",
                newName: "Loc_XX");

            migrationBuilder.RenameColumn(
                name: "TZ",
                table: "ObjectLinks",
                newName: "Loc_TZ");

            migrationBuilder.RenameColumn(
                name: "TY",
                table: "ObjectLinks",
                newName: "Loc_TY");

            migrationBuilder.RenameColumn(
                name: "TX",
                table: "ObjectLinks",
                newName: "Loc_TX");

            migrationBuilder.RenameColumn(
                name: "SoundZZ",
                table: "ObjectLinks",
                newName: "SoundLoc_ZZ");

            migrationBuilder.RenameColumn(
                name: "SoundZY",
                table: "ObjectLinks",
                newName: "SoundLoc_ZY");

            migrationBuilder.RenameColumn(
                name: "SoundZX",
                table: "ObjectLinks",
                newName: "SoundLoc_ZX");

            migrationBuilder.RenameColumn(
                name: "SoundYZ",
                table: "ObjectLinks",
                newName: "SoundLoc_YZ");

            migrationBuilder.RenameColumn(
                name: "SoundYY",
                table: "ObjectLinks",
                newName: "SoundLoc_YY");

            migrationBuilder.RenameColumn(
                name: "SoundYX",
                table: "ObjectLinks",
                newName: "SoundLoc_YX");

            migrationBuilder.RenameColumn(
                name: "SoundXZ",
                table: "ObjectLinks",
                newName: "SoundLoc_XZ");

            migrationBuilder.RenameColumn(
                name: "SoundXY",
                table: "ObjectLinks",
                newName: "SoundLoc_XY");

            migrationBuilder.RenameColumn(
                name: "SoundXX",
                table: "ObjectLinks",
                newName: "SoundLoc_XX");

            migrationBuilder.RenameColumn(
                name: "SoundTZ",
                table: "ObjectLinks",
                newName: "SoundLoc_TZ");

            migrationBuilder.RenameColumn(
                name: "SoundTY",
                table: "ObjectLinks",
                newName: "SoundLoc_TY");

            migrationBuilder.RenameColumn(
                name: "SoundTX",
                table: "ObjectLinks",
                newName: "SoundLoc_TX");

            migrationBuilder.AddColumn<float>(
                name: "SpawnLocAir_TX",
                table: "BlockInfos",
                type: "float",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "SpawnLocAir_TY",
                table: "BlockInfos",
                type: "float",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "SpawnLocAir_TZ",
                table: "BlockInfos",
                type: "float",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "SpawnLocAir_XX",
                table: "BlockInfos",
                type: "float",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "SpawnLocAir_XY",
                table: "BlockInfos",
                type: "float",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "SpawnLocAir_XZ",
                table: "BlockInfos",
                type: "float",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "SpawnLocAir_YX",
                table: "BlockInfos",
                type: "float",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "SpawnLocAir_YY",
                table: "BlockInfos",
                type: "float",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "SpawnLocAir_YZ",
                table: "BlockInfos",
                type: "float",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "SpawnLocAir_ZX",
                table: "BlockInfos",
                type: "float",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "SpawnLocAir_ZY",
                table: "BlockInfos",
                type: "float",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "SpawnLocAir_ZZ",
                table: "BlockInfos",
                type: "float",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "SpawnLocGround_TX",
                table: "BlockInfos",
                type: "float",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "SpawnLocGround_TY",
                table: "BlockInfos",
                type: "float",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "SpawnLocGround_TZ",
                table: "BlockInfos",
                type: "float",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "SpawnLocGround_XX",
                table: "BlockInfos",
                type: "float",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "SpawnLocGround_XY",
                table: "BlockInfos",
                type: "float",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "SpawnLocGround_XZ",
                table: "BlockInfos",
                type: "float",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "SpawnLocGround_YX",
                table: "BlockInfos",
                type: "float",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "SpawnLocGround_YY",
                table: "BlockInfos",
                type: "float",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "SpawnLocGround_YZ",
                table: "BlockInfos",
                type: "float",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "SpawnLocGround_ZX",
                table: "BlockInfos",
                type: "float",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "SpawnLocGround_ZY",
                table: "BlockInfos",
                type: "float",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "SpawnLocGround_ZZ",
                table: "BlockInfos",
                type: "float",
                nullable: false,
                defaultValue: 0f);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SpawnLocAir_TX",
                table: "BlockInfos");

            migrationBuilder.DropColumn(
                name: "SpawnLocAir_TY",
                table: "BlockInfos");

            migrationBuilder.DropColumn(
                name: "SpawnLocAir_TZ",
                table: "BlockInfos");

            migrationBuilder.DropColumn(
                name: "SpawnLocAir_XX",
                table: "BlockInfos");

            migrationBuilder.DropColumn(
                name: "SpawnLocAir_XY",
                table: "BlockInfos");

            migrationBuilder.DropColumn(
                name: "SpawnLocAir_XZ",
                table: "BlockInfos");

            migrationBuilder.DropColumn(
                name: "SpawnLocAir_YX",
                table: "BlockInfos");

            migrationBuilder.DropColumn(
                name: "SpawnLocAir_YY",
                table: "BlockInfos");

            migrationBuilder.DropColumn(
                name: "SpawnLocAir_YZ",
                table: "BlockInfos");

            migrationBuilder.DropColumn(
                name: "SpawnLocAir_ZX",
                table: "BlockInfos");

            migrationBuilder.DropColumn(
                name: "SpawnLocAir_ZY",
                table: "BlockInfos");

            migrationBuilder.DropColumn(
                name: "SpawnLocAir_ZZ",
                table: "BlockInfos");

            migrationBuilder.DropColumn(
                name: "SpawnLocGround_TX",
                table: "BlockInfos");

            migrationBuilder.DropColumn(
                name: "SpawnLocGround_TY",
                table: "BlockInfos");

            migrationBuilder.DropColumn(
                name: "SpawnLocGround_TZ",
                table: "BlockInfos");

            migrationBuilder.DropColumn(
                name: "SpawnLocGround_XX",
                table: "BlockInfos");

            migrationBuilder.DropColumn(
                name: "SpawnLocGround_XY",
                table: "BlockInfos");

            migrationBuilder.DropColumn(
                name: "SpawnLocGround_XZ",
                table: "BlockInfos");

            migrationBuilder.DropColumn(
                name: "SpawnLocGround_YX",
                table: "BlockInfos");

            migrationBuilder.DropColumn(
                name: "SpawnLocGround_YY",
                table: "BlockInfos");

            migrationBuilder.DropColumn(
                name: "SpawnLocGround_YZ",
                table: "BlockInfos");

            migrationBuilder.DropColumn(
                name: "SpawnLocGround_ZX",
                table: "BlockInfos");

            migrationBuilder.DropColumn(
                name: "SpawnLocGround_ZY",
                table: "BlockInfos");

            migrationBuilder.DropColumn(
                name: "SpawnLocGround_ZZ",
                table: "BlockInfos");

            migrationBuilder.RenameColumn(
                name: "Loc_ZZ",
                table: "ObjectLinks",
                newName: "ZZ");

            migrationBuilder.RenameColumn(
                name: "Loc_ZY",
                table: "ObjectLinks",
                newName: "ZY");

            migrationBuilder.RenameColumn(
                name: "Loc_ZX",
                table: "ObjectLinks",
                newName: "ZX");

            migrationBuilder.RenameColumn(
                name: "Loc_YZ",
                table: "ObjectLinks",
                newName: "YZ");

            migrationBuilder.RenameColumn(
                name: "Loc_YY",
                table: "ObjectLinks",
                newName: "YY");

            migrationBuilder.RenameColumn(
                name: "Loc_YX",
                table: "ObjectLinks",
                newName: "YX");

            migrationBuilder.RenameColumn(
                name: "Loc_XZ",
                table: "ObjectLinks",
                newName: "XZ");

            migrationBuilder.RenameColumn(
                name: "Loc_XY",
                table: "ObjectLinks",
                newName: "XY");

            migrationBuilder.RenameColumn(
                name: "Loc_XX",
                table: "ObjectLinks",
                newName: "XX");

            migrationBuilder.RenameColumn(
                name: "Loc_TZ",
                table: "ObjectLinks",
                newName: "TZ");

            migrationBuilder.RenameColumn(
                name: "Loc_TY",
                table: "ObjectLinks",
                newName: "TY");

            migrationBuilder.RenameColumn(
                name: "Loc_TX",
                table: "ObjectLinks",
                newName: "TX");

            migrationBuilder.RenameColumn(
                name: "SoundLoc_ZZ",
                table: "ObjectLinks",
                newName: "SoundZZ");

            migrationBuilder.RenameColumn(
                name: "SoundLoc_ZY",
                table: "ObjectLinks",
                newName: "SoundZY");

            migrationBuilder.RenameColumn(
                name: "SoundLoc_ZX",
                table: "ObjectLinks",
                newName: "SoundZX");

            migrationBuilder.RenameColumn(
                name: "SoundLoc_YZ",
                table: "ObjectLinks",
                newName: "SoundYZ");

            migrationBuilder.RenameColumn(
                name: "SoundLoc_YY",
                table: "ObjectLinks",
                newName: "SoundYY");

            migrationBuilder.RenameColumn(
                name: "SoundLoc_YX",
                table: "ObjectLinks",
                newName: "SoundYX");

            migrationBuilder.RenameColumn(
                name: "SoundLoc_XZ",
                table: "ObjectLinks",
                newName: "SoundXZ");

            migrationBuilder.RenameColumn(
                name: "SoundLoc_XY",
                table: "ObjectLinks",
                newName: "SoundXY");

            migrationBuilder.RenameColumn(
                name: "SoundLoc_XX",
                table: "ObjectLinks",
                newName: "SoundXX");

            migrationBuilder.RenameColumn(
                name: "SoundLoc_TZ",
                table: "ObjectLinks",
                newName: "SoundTZ");

            migrationBuilder.RenameColumn(
                name: "SoundLoc_TY",
                table: "ObjectLinks",
                newName: "SoundTY");

            migrationBuilder.RenameColumn(
                name: "SoundLoc_TX",
                table: "ObjectLinks",
                newName: "SoundTX");
        }
    }
}
