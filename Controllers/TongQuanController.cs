using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using Web_quan_ly_nhan_su.Context;

namespace Web_quan_ly_nhan_su.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TongQuanController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TongQuanController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("thong-ke")]
        public async Task<IActionResult> GetThongKeDashBoard()
        {
            try
            {
                // 👉 1. LẤY ID NHÂN VIÊN ĐANG ĐĂNG NHẬP THỰC TẾ
                int currentUserId = HttpContext.Session.GetInt32("MaNhanVien") ?? 0;
                if (currentUserId == 0)
                {
                    var claim = User.Claims.FirstOrDefault(c => c.Type == "MaNhanVien")?.Value
                             ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                    int.TryParse(claim, out currentUserId);
                }

                // Backup trong trường hợp test chưa đăng nhập
                if (currentUserId == 0) currentUserId = 1;

                // 2. Tổng nhân viên và Phòng ban (Vẫn giữ nguyên cho toàn công ty)
                int tongNhanVien = await _context.NhanVien.CountAsync();
                int tongPhongBan = await _context.PhongBan.CountAsync();

                // 3. Xử lý logic Chấm công
                DateTime vietnamTime = DateTime.UtcNow.AddHours(7);
                DateTime homNay = DateTime.SpecifyKind(vietnamTime.Date, DateTimeKind.Utc);

                // 👉 TÍNH SỐ NGÀY CHƯA CHẤM CÔNG CỦA CÁ NHÂN (TRONG THÁNG NÀY)
                int soNgayDaQuaTrongThang = homNay.Day;
                int soNgayDaChamCong = await _context.ChamCong
                    .Where(c => c.MaNhanVien == currentUserId && c.NgayLamViec.Month == homNay.Month && c.NgayLamViec.Year == homNay.Year)
                    .CountAsync();

                // Số ngày vắng = Tổng ngày đã qua - Số ngày có đi làm
                int chuaChamCong = Math.Max(0, soNgayDaQuaTrongThang - soNgayDaChamCong);

                // Tỷ lệ đi đúng giờ (Vẫn giữ chung cho toàn công ty trong ngày hôm nay)
                var danhSachChamCongHnay = await _context.ChamCong
                    .Where(c => c.NgayLamViec == homNay && c.GioVao.HasValue)
                    .ToListAsync();
                int soNguoiDaChamCong = danhSachChamCongHnay.Count;
                TimeSpan gioVaoChuan = new TimeSpan(8, 0, 0);
                int diDungGio = danhSachChamCongHnay.Count(c => c.GioVao.Value <= gioVaoChuan);
                double tyLeDungGio = soNguoiDaChamCong > 0 ? Math.Round((double)diDungGio / soNguoiDaChamCong * 100, 1) : 0;

                // 👉 4. LẤY LƯƠNG GẦN NHẤT CỦA CÁ NHÂN
                string luongGanNhat = "0 VNĐ";
                var checkLuong = await _context.Luong
                    .Where(l => l.MaNhanVien == currentUserId)
                    .OrderByDescending(l => l.Nam)
                    .ThenByDescending(l => l.Thang)
                    .FirstOrDefaultAsync();

                if (checkLuong != null)
                {
                    // Lấy TongLuong và định dạng tiền tệ
                    luongGanNhat = string.Format("{0:N0} VNĐ", checkLuong.TongLuong);
                }

                // =========================================================
                // 5. LẤY TIN NHẮN MỚI NHẤT
                // =========================================================
                var myGroupIds = await _context.ThanhVienNhom
                    .Where(tv => tv.MaNhanVien == currentUserId)
                    .Select(tv => tv.MaNhom)
                    .ToListAsync();

                DateTime mốc2GioTruoc = DateTime.UtcNow.AddHours(-2);

                var rawMessages = await _context.TinNhan
                    .Include(t => t.NguoiGui)
                    .Where(t =>
                        (t.NguoiNhanId == currentUserId || (t.MaNhom != null && myGroupIds.Contains(t.MaNhom.Value)))
                        && t.NguoiGuiId != currentUserId
                        && t.ThoiGianGui >= mốc2GioTruoc
                    )
                    .OrderByDescending(t => t.ThoiGianGui)
                    .Take(5)
                    .ToListAsync();

                var tinNhanMoi = rawMessages.Select(t => new
                {
                    nguoiGuiId = t.NguoiGuiId,
                    maNhom = t.MaNhom,
                    nguoiGui = t.NguoiGui != null ? t.NguoiGui.HoTen : "Hệ thống",
                    anhDaiDien = t.NguoiGui != null ? t.NguoiGui.AnhDaiDien : "/images/avatar_default.jpg",
                    noiDung = t.NoiDung.StartsWith("[FILE]")
                                ? "📎 Đã gửi một tệp đính kèm"
                                : (t.NoiDung.Length > 45 ? t.NoiDung.Substring(0, 45) + "..." : t.NoiDung),
                    thoiGian = TinhThoiGianTuongDoi(t.ThoiGianGui)
                }).ToList();

                // TRẢ DỮ LIỆU VỀ GIAO DIỆN
                return Ok(new
                {
                    success = true,
                    tongNhanVien,
                    tongPhongBan,
                    chuaChamCong,   // Trả về số ngày cá nhân đó chưa chấm công trong tháng
                    tyLeDungGio,
                    luongGanNhat,   // Mở comment và trả về lương cá nhân
                    tinNhanMoi
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        private static string TinhThoiGianTuongDoi(DateTime thoiGianUtc)
        {
            var span = DateTime.UtcNow - thoiGianUtc;
            if (span.TotalMinutes < 1) return "Vừa xong";
            if (span.TotalMinutes < 60) return (int)span.TotalMinutes + " phút trước";
            if (span.TotalHours < 24) return (int)span.TotalHours + " giờ trước";
            return thoiGianUtc.AddHours(7).ToString("dd/MM/yyyy HH:mm");
        }
    }
}