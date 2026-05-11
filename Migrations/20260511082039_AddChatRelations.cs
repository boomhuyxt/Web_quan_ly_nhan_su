using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Web_quan_ly_nhan_su.Migrations
{
    /// <inheritdoc />
    public partial class AddChatRelations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "NguoiNhanId",
                table: "TinNhan",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.CreateIndex(
                name: "IX_TinNhan_MaNhom",
                table: "TinNhan",
                column: "MaNhom");

            migrationBuilder.CreateIndex(
                name: "IX_TinNhan_NguoiGuiId",
                table: "TinNhan",
                column: "NguoiGuiId");

            migrationBuilder.CreateIndex(
                name: "IX_TinNhan_NguoiNhanId",
                table: "TinNhan",
                column: "NguoiNhanId");

            migrationBuilder.CreateIndex(
                name: "IX_ThanhVienNhom_MaNhanVien",
                table: "ThanhVienNhom",
                column: "MaNhanVien");

            migrationBuilder.CreateIndex(
                name: "IX_ThanhVienNhom_MaNhom",
                table: "ThanhVienNhom",
                column: "MaNhom");

            migrationBuilder.CreateIndex(
                name: "IX_NhomChat_NguoiTaoId",
                table: "NhomChat",
                column: "NguoiTaoId");

            migrationBuilder.AddForeignKey(
                name: "FK_NhomChat_NhanVien_NguoiTaoId",
                table: "NhomChat",
                column: "NguoiTaoId",
                principalTable: "NhanVien",
                principalColumn: "MaNhanVien",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ThanhVienNhom_NhanVien_MaNhanVien",
                table: "ThanhVienNhom",
                column: "MaNhanVien",
                principalTable: "NhanVien",
                principalColumn: "MaNhanVien",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ThanhVienNhom_NhomChat_MaNhom",
                table: "ThanhVienNhom",
                column: "MaNhom",
                principalTable: "NhomChat",
                principalColumn: "MaNhom",
                onDelete: ReferentialAction.Cascade);

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

            migrationBuilder.AddForeignKey(
                name: "FK_TinNhan_NhomChat_MaNhom",
                table: "TinNhan",
                column: "MaNhom",
                principalTable: "NhomChat",
                principalColumn: "MaNhom");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_NhomChat_NhanVien_NguoiTaoId",
                table: "NhomChat");

            migrationBuilder.DropForeignKey(
                name: "FK_ThanhVienNhom_NhanVien_MaNhanVien",
                table: "ThanhVienNhom");

            migrationBuilder.DropForeignKey(
                name: "FK_ThanhVienNhom_NhomChat_MaNhom",
                table: "ThanhVienNhom");

            migrationBuilder.DropForeignKey(
                name: "FK_TinNhan_NhanVien_NguoiGuiId",
                table: "TinNhan");

            migrationBuilder.DropForeignKey(
                name: "FK_TinNhan_NhanVien_NguoiNhanId",
                table: "TinNhan");

            migrationBuilder.DropForeignKey(
                name: "FK_TinNhan_NhomChat_MaNhom",
                table: "TinNhan");

            migrationBuilder.DropIndex(
                name: "IX_TinNhan_MaNhom",
                table: "TinNhan");

            migrationBuilder.DropIndex(
                name: "IX_TinNhan_NguoiGuiId",
                table: "TinNhan");

            migrationBuilder.DropIndex(
                name: "IX_TinNhan_NguoiNhanId",
                table: "TinNhan");

            migrationBuilder.DropIndex(
                name: "IX_ThanhVienNhom_MaNhanVien",
                table: "ThanhVienNhom");

            migrationBuilder.DropIndex(
                name: "IX_ThanhVienNhom_MaNhom",
                table: "ThanhVienNhom");

            migrationBuilder.DropIndex(
                name: "IX_NhomChat_NguoiTaoId",
                table: "NhomChat");

            migrationBuilder.AlterColumn<int>(
                name: "NguoiNhanId",
                table: "TinNhan",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }
    }
}
