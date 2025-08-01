﻿// <auto-generated />
using System;
using GbxTools3D.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace GbxTools3D.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20250211000729_LengthTweaksIndexing")]
    partial class LengthTweaksIndexing
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.10")
                .HasAnnotation("Relational:MaxIdentifierLength", 64);

            MySqlModelBuilderExtensions.AutoIncrementColumns(modelBuilder);

            modelBuilder.Entity("GbxTools3D.Data.Entities.BlockInfo", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("AirUnits")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<int>("CollectionId")
                        .HasColumnType("int");

                    b.Property<string>("GroundUnits")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<bool>("HasAirHelper")
                        .HasColumnType("tinyint(1)");

                    b.Property<bool>("HasConstructionModeHelper")
                        .HasColumnType("tinyint(1)");

                    b.Property<bool>("HasGroundHelper")
                        .HasColumnType("tinyint(1)");

                    b.Property<byte?>("Height")
                        .HasColumnType("tinyint unsigned");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(64)
                        .HasColumnType("varchar(64)");

                    b.HasKey("Id");

                    b.HasIndex("CollectionId");

                    b.HasIndex("Name");

                    b.ToTable("BlockInfos");
                });

            modelBuilder.Entity("GbxTools3D.Data.Entities.BlockVariant", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<int>("BlockInfoId")
                        .HasColumnType("int");

                    b.Property<bool>("Ground")
                        .HasColumnType("tinyint(1)");

                    b.Property<int>("MeshId")
                        .HasColumnType("int");

                    b.Property<string>("Path")
                        .HasMaxLength(255)
                        .HasColumnType("varchar(255)");

                    b.Property<byte>("SubVariant")
                        .HasColumnType("tinyint unsigned");

                    b.Property<byte>("Variant")
                        .HasColumnType("tinyint unsigned");

                    b.HasKey("Id");

                    b.HasIndex("BlockInfoId");

                    b.HasIndex("MeshId");

                    b.ToTable("BlockVariants");
                });

            modelBuilder.Entity("GbxTools3D.Data.Entities.Collection", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("DefaultZoneBlock")
                        .HasMaxLength(255)
                        .HasColumnType("varchar(255)");

                    b.Property<string>("DisplayName")
                        .HasMaxLength(255)
                        .HasColumnType("varchar(255)");

                    b.Property<int>("GameVersion")
                        .HasColumnType("int");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("varchar(255)");

                    b.Property<int>("SortIndex")
                        .HasColumnType("int");

                    b.Property<int>("SquareHeight")
                        .HasColumnType("int");

                    b.Property<int>("SquareSize")
                        .HasColumnType("int");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("VehicleAuthor")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("varchar(255)");

                    b.Property<string>("VehicleCollection")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("varchar(255)");

                    b.Property<string>("VehicleId")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("varchar(255)");

                    b.HasKey("Id");

                    b.ToTable("Collections");
                });

            modelBuilder.Entity("GbxTools3D.Data.Entities.Decoration", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<int>("DecorationSizeId")
                        .HasColumnType("int");

                    b.Property<string>("Musics")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(32)
                        .HasColumnType("varchar(32)");

                    b.Property<string>("Remap")
                        .HasMaxLength(255)
                        .HasColumnType("varchar(255)");

                    b.Property<string>("Sounds")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.HasKey("Id");

                    b.HasIndex("DecorationSizeId");

                    b.HasIndex("Name");

                    b.ToTable("Decorations");
                });

            modelBuilder.Entity("GbxTools3D.Data.Entities.DecorationSize", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<int>("BaseHeight")
                        .HasColumnType("int");

                    b.Property<int>("CollectionId")
                        .HasColumnType("int");

                    b.Property<string>("Scene")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<int>("SizeX")
                        .HasColumnType("int");

                    b.Property<int>("SizeY")
                        .HasColumnType("int");

                    b.Property<int>("SizeZ")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("CollectionId");

                    b.HasIndex("SizeX", "SizeY", "SizeZ");

                    b.ToTable("DecorationSizes");
                });

            modelBuilder.Entity("GbxTools3D.Data.Entities.Mesh", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<byte[]>("Data")
                        .IsRequired()
                        .HasMaxLength(16777215)
                        .HasColumnType("longblob");

                    b.Property<byte[]>("DataLQ")
                        .HasMaxLength(16777215)
                        .HasColumnType("longblob");

                    b.Property<byte[]>("DataSurf")
                        .HasMaxLength(16777215)
                        .HasColumnType("longblob");

                    b.Property<byte[]>("DataVLQ")
                        .HasMaxLength(16777215)
                        .HasColumnType("longblob");

                    b.Property<string>("Hash")
                        .IsRequired()
                        .HasMaxLength(64)
                        .HasColumnType("varchar(64)");

                    b.Property<string>("Path")
                        .HasMaxLength(255)
                        .HasColumnType("varchar(255)");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("datetime(6)");

                    b.HasKey("Id");

                    b.HasIndex("Hash")
                        .IsUnique();

                    b.ToTable("Meshes");
                });

            modelBuilder.Entity("GbxTools3D.Data.Entities.ObjectLink", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<int>("Index")
                        .HasColumnType("int");

                    b.Property<int>("MeshId")
                        .HasColumnType("int");

                    b.Property<string>("Path")
                        .HasMaxLength(255)
                        .HasColumnType("varchar(255)");

                    b.Property<float>("TX")
                        .HasColumnType("float");

                    b.Property<float>("TY")
                        .HasColumnType("float");

                    b.Property<float>("TZ")
                        .HasColumnType("float");

                    b.Property<int>("VariantId")
                        .HasColumnType("int");

                    b.Property<float>("XX")
                        .HasColumnType("float");

                    b.Property<float>("XY")
                        .HasColumnType("float");

                    b.Property<float>("XZ")
                        .HasColumnType("float");

                    b.Property<float>("YX")
                        .HasColumnType("float");

                    b.Property<float>("YY")
                        .HasColumnType("float");

                    b.Property<float>("YZ")
                        .HasColumnType("float");

                    b.Property<float>("ZX")
                        .HasColumnType("float");

                    b.Property<float>("ZY")
                        .HasColumnType("float");

                    b.Property<float>("ZZ")
                        .HasColumnType("float");

                    b.HasKey("Id");

                    b.HasIndex("MeshId");

                    b.HasIndex("VariantId");

                    b.ToTable("ObjectLinks");
                });

            modelBuilder.Entity("GbxTools3D.Data.Entities.BlockInfo", b =>
                {
                    b.HasOne("GbxTools3D.Data.Entities.Collection", "Collection")
                        .WithMany("BlockInfos")
                        .HasForeignKey("CollectionId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Collection");
                });

            modelBuilder.Entity("GbxTools3D.Data.Entities.BlockVariant", b =>
                {
                    b.HasOne("GbxTools3D.Data.Entities.BlockInfo", "BlockInfo")
                        .WithMany("Variants")
                        .HasForeignKey("BlockInfoId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("GbxTools3D.Data.Entities.Mesh", "Mesh")
                        .WithMany()
                        .HasForeignKey("MeshId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("BlockInfo");

                    b.Navigation("Mesh");
                });

            modelBuilder.Entity("GbxTools3D.Data.Entities.Decoration", b =>
                {
                    b.HasOne("GbxTools3D.Data.Entities.DecorationSize", "DecorationSize")
                        .WithMany("Decorations")
                        .HasForeignKey("DecorationSizeId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("DecorationSize");
                });

            modelBuilder.Entity("GbxTools3D.Data.Entities.DecorationSize", b =>
                {
                    b.HasOne("GbxTools3D.Data.Entities.Collection", "Collection")
                        .WithMany()
                        .HasForeignKey("CollectionId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Collection");
                });

            modelBuilder.Entity("GbxTools3D.Data.Entities.ObjectLink", b =>
                {
                    b.HasOne("GbxTools3D.Data.Entities.Mesh", "Mesh")
                        .WithMany()
                        .HasForeignKey("MeshId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("GbxTools3D.Data.Entities.BlockVariant", "Variant")
                        .WithMany("ObjectLinks")
                        .HasForeignKey("VariantId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Mesh");

                    b.Navigation("Variant");
                });

            modelBuilder.Entity("GbxTools3D.Data.Entities.BlockInfo", b =>
                {
                    b.Navigation("Variants");
                });

            modelBuilder.Entity("GbxTools3D.Data.Entities.BlockVariant", b =>
                {
                    b.Navigation("ObjectLinks");
                });

            modelBuilder.Entity("GbxTools3D.Data.Entities.Collection", b =>
                {
                    b.Navigation("BlockInfos");
                });

            modelBuilder.Entity("GbxTools3D.Data.Entities.DecorationSize", b =>
                {
                    b.Navigation("Decorations");
                });
#pragma warning restore 612, 618
        }
    }
}
