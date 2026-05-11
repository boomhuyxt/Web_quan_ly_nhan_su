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
                // Giả định ID user đang đăng nhập là 1 
                int currentUserId = 1;

                // 1. Tổng nhân viên và Phòng ban
                int tongNhanVien = await _context.NhanVien.CountAsync();
                int tongPhongBan = await _context.PhongBan.CountAsync();

                // 2. Xử lý logic Chấm công (Mốc 8:00 AM)
                DateTime vietnamTime = DateTime.UtcNow.AddHours(7);
                DateTime homNay = DateTime.SpecifyKind(vietnamTime.Date, DateTimeKind.Utc);

                var danhSachChamCong = await _context.ChamCong
                    .Where(c => c.NgayLamViec == homNay && c.GioVao.HasValue)
                    .ToListAsync();

                int soNguoiDaChamCong = danhSachChamCong.Count;
                int chuaChamCong = Math.Max(0, tongNhanVien - soNguoiDaChamCong);

                TimeSpan gioVaoChuan = new TimeSpan(8, 0, 0);
                int diDungGio = danhSachChamCong.Count(c => c.GioVao.Value <= gioVaoChuan);

                double tyLeDungGio = soNguoiDaChamCong > 0
                    ? Math.Round((double)diDungGio / soNguoiDaChamCong * 100, 1)
                    : 0;

                // 3. Lấy Lương gần nhất của User
                //string luongGanNhat = "0 VNĐ";
                //var checkLuong = await _context.Luong
                //    .Where(l => l.MaNhanVien == currentUserId)
                //    .OrderByDescending(l => l.Id) // Thay 'Id' bằng khóa chính bảng Lương của bạn nếu bị lỗi
                //    .FirstOrDefaultAsync();

                //if (checkLuong != null)
                //{
                //    luongGanNhat = string.Format("{0:N0} đ", 15000000); // Thay 15000000 bằng số tiền thực tế
                //}

                // =========================================================
                // 4. LẤY TIN NHẮN MỚI NHẤT DO NGƯỜI KHÁC GỬI TRONG 2 GIỜ QUA
                // =========================================================
                var myGroupIds = await _context.ThanhVienNhom
                    .Where(tv => tv.MaNhanVien == currentUserId)
                    .Select(tv => tv.MaNhom)
                    .ToListAsync();

                // Mốc thời gian 2 giờ trước tính theo giờ chuẩn UTC
                DateTime mốc2GioTruoc = DateTime.UtcNow.AddHours(-2);

                // Kéo dữ liệu từ CSDL lên RAM trước để tránh lỗi EF Core khi dùng Substring
                var rawMessages = await _context.TinNhan
                    .Include(t => t.NguoiGui)
                    .Where(t =>
                        // 1. Nhận được cá nhân hoặc nằm trong nhóm của mình
                        (t.NguoiNhanId == currentUserId || (t.MaNhom != null && myGroupIds.Contains(t.MaNhom.Value)))
                        // 2. Phải là NGƯỜI KHÁC gửi cho mình
                        && t.NguoiGuiId != currentUserId
                        // 3. Gửi trong vòng 2 tiếng trở lại đây
                        && t.ThoiGianGui >= mốc2GioTruoc
                    )
                    .OrderByDescending(t => t.ThoiGianGui)
                    .Take(5) // Lấy tối đa 5 tin để giao diện không bị tràn
                    .ToListAsync();

                // Định dạng lại nội dung
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

                return Ok(new
                {
                    success = true,
                    tongNhanVien,
                    tongPhongBan,
                    chuaChamCong,
                    tyLeDungGio,
                    //luongGanNhat,
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