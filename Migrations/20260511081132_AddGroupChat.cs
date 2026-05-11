using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Web_quan_ly_nhan_su.Migrations
{
    /// <inheritdoc />
    public partial class AddGroupChat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaNhom",
                table: "TinNhan",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "NhomChat",
                columns: table => new
                {
                    MaNhom = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenNhom = table.Column<string>(type: "text", nullable: false),
                    NguoiTaoId = table.Column<int>(type: "integer", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AnhNhom = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NhomChat", x => x.MaNhom);
                });

            migrationBuilder.CreateTable(
                name: "ThanhVienNhom",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MaNhom = table.Column<int>(type: "integer", nullable: false),
                    MaNhanVien = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThanhVienNhom", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NhomChat");

            migrationBuilder.DropTable(
                name: "ThanhVienNhom");

            migrationBuilder.DropColumn(
                name: "MaNhom",
                table: "TinNhan");
        }
    }
}
