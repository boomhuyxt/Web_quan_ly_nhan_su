using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Web_quan_ly_nhan_su.Migrations
{
    /// <inheritdoc />
    public partial class HoanThienDatabaseChat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TinNhan_NhanVien_NguoiGuiId",
                table: "TinNhan");

            migrationBuilder.DropForeignKey(
                name: "FK_TinNhan_NhanVien_NguoiNhanId",
                table: "TinNhan");

            migrationBuilder.CreateTable(
                name: "TinNhanNhom",
                columns: table => new
                {
                    MaTinNhan = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MaNhom = table.Column<int>(type: "integer", nullable: false),
                    NguoiGuiId = table.Column<int>(type: "integer", nullable: false),
                    NoiDung = table.Column<string>(type: "text", nullable: false),
                    ThoiGianGui = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TinNhanNhom", x => x.MaTinNhan);
                    table.ForeignKey(
                        name: "FK_TinNhanNhom_NhanVien_NguoiGuiId",
                        column: x => x.NguoiGuiId,
                        principalTable: "NhanVien",
                        principalColumn: "MaNhanVien",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TinNhanNhom_NhomChat_MaNhom",
                        column: x => x.MaNhom,
                        principalTable: "NhomChat",
                        principalColumn: "MaNhom",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TinNhanNhom_MaNhom",
                table: "TinNhanNhom",
                column: "MaNhom");

            migrationBuilder.CreateIndex(
                name: "IX_TinNhanNhom_NguoiGuiId",
                table: "TinNhanNhom",
                column: "NguoiGuiId");

            migrationBuilder.AddForeignKey(
                name: "FK_TinNhan_NhanVien_NguoiGuiId",
                table: "TinNhan",
                column: "NguoiGuiId",
                principalTable: "NhanVien",
                principalColumn: "MaNhanVien",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TinNhan_NhanVien_NguoiNhanId",
                table: "TinNhan",
                column: "NguoiNhanId",
                principalTable: "NhanVien",
                principalColumn: "MaNhanVien",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TinNhan_NhanVien_NguoiGuiId",
                table: "TinNhan");

            migrationBuilder.DropForeignKey(
                name: "FK_TinNhan_NhanVien_NguoiNhanId",
                table: "TinNhan");

            migrationBuilder.DropTable(
                name: "TinNhanNhom");

            migrationBuilder.AddForeignKey(
                name: "FK_TinNhan_NhanVien_NguoiGuiId",
                table: "TinNhan",
                column: "NguoiGuiId",
                principalTable: "NhanVien",
                principalColumn: "MaNhanVien",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TinNhan_NhanVien_NguoiNhanId",
                table: "TinNhan",
                column: "NguoiNhanId",
                principalTable: "NhanVien",
                principalColumn: "MaNhanVien");
        }
    }
}
