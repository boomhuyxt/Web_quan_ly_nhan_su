using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using Web_quan_ly_nhan_su.Context;
using Web_quan_ly_nhan_su.Models;

namespace Web_quan_ly_nhan_su.Controllers
{
    public class LuongController : Controller
    {
        private readonly AppDbContext _context;

        public LuongController(AppDbContext context)
        {
            _context = context;
        }

        // ====================================================================
        // 1. GIAO DIỆN: XEM TẤT CẢ BẢNG LƯƠNG TRONG DATA (DÀNH CHO ADMIN)
        // ====================================================================
        [HttpGet]
        public async Task<IActionResult> DanhSachToanCongTy() // Đổi tên hàm cho đúng nghiệp vụ
        {
            // 👉 Lấy TẤT CẢ dữ liệu có trong bảng Luong (Không có lệnh .Where để lọc)
            // 👉 Dùng .Include() để Entity Framework tự động lấy thêm Tên và Ảnh của nhân viên
            var danhSachLuong = await _context.Luong
                .Include(l => l.NhanVien)
                .OrderByDescending(l => l.Nam)
                .ThenByDescending(l => l.Thang)
                .ToListAsync();

            // Trả toàn bộ data ra giao diện Luong.cshtml
            return View("~/Views/Home/Luong.cshtml", danhSachLuong);
        }

        // ====================================================================
        // 2. API: TÍNH TOÁN VÀ LƯU LƯƠNG
        // ====================================================================
        [HttpPost("api/luong/tinh-luong")]
        public async Task<IActionResult> TinhVaLuuLuong([FromBody] NhapLuongRequest request)
        {
            try
            {
                if (request.MaNhanVien <= 0) return BadRequest(new { success = false, message = "Vui lòng chọn nhân viên." });
                if (request.Thang < 1 || request.Thang > 12) return BadRequest(new { success = false, message = "Tháng không hợp lệ." });

                var nhanVien = await _context.NhanVien.FindAsync(request.MaNhanVien);
                if (nhanVien == null) return NotFound(new { success = false, message = "Không tìm thấy nhân viên." });

                // 👉 1. TÍNH BẢO HIỂM (Từ Lương Cơ Bản)
                decimal bhxh = request.LuongCoBan * 0.08m;   // 8%
                decimal bhyt = request.LuongCoBan * 0.015m;  // 1.5%
                decimal bhtn = request.LuongCoBan * 0.01m;   // 1.0%

                // 👉 2. TÍNH TIỀN TĂNG CA (Nhân hệ số 1.2)
                decimal tienTangCa = 0;
                if (request.SoGioTangCa > 0)
                {
                    tienTangCa = (request.LuongCoBan / 26m / 8m) * request.SoGioTangCa * 1.2m;
                }

                // 👉 3. TÍNH TỔNG LƯƠNG
                decimal tongLuongTinhToan = request.LuongCoBan + tienTangCa + request.Thuong - request.KhauTru - bhxh - bhyt - bhtn;
                if (tongLuongTinhToan < 0) tongLuongTinhToan = 0;

                // Kiểm tra xem đã có bản ghi của tháng/năm này chưa
                var banGhiLuong = await _context.Luong
                    .FirstOrDefaultAsync(l => l.MaNhanVien == request.MaNhanVien && l.Thang == request.Thang && l.Nam == request.Nam);

                if (banGhiLuong == null)
                {
                    var luongMoi = new Luong
                    {
                        MaNhanVien = request.MaNhanVien,
                        Thang = request.Thang,
                        Nam = request.Nam,
                        LuongCoBan = request.LuongCoBan,
                        Thuong = request.Thuong,
                        KhauTru = request.KhauTru,
                        BaoHiemXaHoi = bhxh,
                        BaoHiemYTe = bhyt,
                        BaoHiemThatNghiep = bhtn,
                        TienTangCa = Math.Round(tienTangCa),
                        TongLuong = Math.Round(tongLuongTinhToan)
                    };
                    _context.Luong.Add(luongMoi);
                }
                else
                {
                    banGhiLuong.LuongCoBan = request.LuongCoBan;
                    banGhiLuong.Thuong = request.Thuong;
                    banGhiLuong.KhauTru = request.KhauTru;
                    banGhiLuong.BaoHiemXaHoi = bhxh;
                    banGhiLuong.BaoHiemYTe = bhyt;
                    banGhiLuong.BaoHiemThatNghiep = bhtn;
                    banGhiLuong.TienTangCa = Math.Round(tienTangCa);
                    banGhiLuong.TongLuong = Math.Round(tongLuongTinhToan);

                    _context.Luong.Update(banGhiLuong);
                }

                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Đã lưu và tính toán lương thành công!", tongLuong = Math.Round(tongLuongTinhToan) });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }
    }

    public class NhapLuongRequest
    {
        public int MaNhanVien { get; set; }
        public int Thang { get; set; }
        public int Nam { get; set; }
        public decimal LuongCoBan { get; set; }
        public decimal SoGioTangCa { get; set; }
        public decimal Thuong { get; set; }
        public decimal KhauTru { get; set; }
    }
}