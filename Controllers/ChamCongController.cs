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
    public class ChamCongController : Controller
    {
        private readonly AppDbContext _context;

        public ChamCongController(AppDbContext context)
        {
            _context = context;
        }

        public class FaceRequest
        {
            public string ImageBase64 { get; set; }
        }

        // =========================================================
        // HÀM HỖ TRỢ: LẤY ID NHÂN VIÊN ĐANG ĐĂNG NHẬP THỰC TẾ
        // =========================================================
        private int GetCurrentUserId()
        {
            // Thử lấy từ Session
            int userId = HttpContext.Session.GetInt32("MaNhanVien") ?? 0;

            // Nếu Session không có, thử lấy từ Cookie Authentication
            if (userId == 0)
            {
                var claim = User.Claims.FirstOrDefault(c => c.Type == "MaNhanVien")?.Value
                         ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                int.TryParse(claim, out userId);
            }

            return userId;
        }

        // 1. API ĐĂNG KÝ KHUÔN MẶT
        [HttpPost]
        public async Task<IActionResult> DangKyKhuonMat([FromBody] FaceRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.ImageBase64))
                    return Json(new { success = false, message = "Không nhận được dữ liệu ảnh." });

                // 👉 ĐÃ SỬA: Lấy ID thật thay vì gán cứng số 1
                int maNhanVienCurrent = GetCurrentUserId();
                if (maNhanVienCurrent <= 0)
                    return Json(new { success = false, message = "Phiên đăng nhập đã hết hạn, vui lòng đăng nhập lại." });

                var nhanVien = await _context.NhanVien.FindAsync(maNhanVienCurrent);
                if (nhanVien == null)
                    return Json(new { success = false, message = "Không tìm thấy thông tin nhân viên." });

                nhanVien.FaceVector = request.ImageBase64;
                _context.NhanVien.Update(nhanVien);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Đăng ký FaceID thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi server: " + ex.Message });
            }
        }

        // 2. API CHẤM CÔNG KHUÔN MẶT
        [HttpPost]
        public async Task<IActionResult> NhanDienVaChamCong([FromBody] FaceRequest request)
        {
            try
            {
                // 👉 ĐÃ SỬA: Lấy ID thật
                int maNhanVienCurrent = GetCurrentUserId();
                if (maNhanVienCurrent <= 0)
                    return Json(new { success = false, message = "Phiên đăng nhập đã hết hạn, vui lòng đăng nhập lại." });

                var nhanVien = await _context.NhanVien.FindAsync(maNhanVienCurrent);

                if (nhanVien == null || string.IsNullOrEmpty(nhanVien.FaceVector))
                    return Json(new { success = false, message = "Bạn chưa đăng ký khuôn mặt!" });

                bool isMatch = true; // Bỏ qua logic AI để test luồng

                if (!isMatch)
                    return Json(new { success = false, message = "Khuôn mặt không khớp!" });

                DateTime vietnamTime = DateTime.UtcNow.AddHours(7);
                DateTime homNay = DateTime.SpecifyKind(vietnamTime.Date, DateTimeKind.Utc);
                TimeSpan gioHienTai = vietnamTime.TimeOfDay;

                var chamCong = await _context.ChamCong
                    .FirstOrDefaultAsync(c => c.MaNhanVien == maNhanVienCurrent && c.NgayLamViec == homNay);

                if (chamCong == null)
                {
                    _context.ChamCong.Add(new ChamCong
                    {
                        MaNhanVien = maNhanVienCurrent,
                        NgayLamViec = homNay,
                        GioVao = gioHienTai
                    });
                }
                else
                {
                    chamCong.GioRa = gioHienTai;
                    _context.ChamCong.Update(chamCong);
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true, time = gioHienTai.ToString(@"hh\:mm") });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi server: " + ex.Message });
            }
        }

        // 3. API LẤY LỊCH SỬ CHẤM CÔNG 
        [HttpGet]
        public async Task<IActionResult> GetLichSuChamCong()
        {
            try
            {
                // 👉 ĐÃ SỬA: Lấy ID thật để lọc đúng lịch sử của nhân viên đó
                int maNhanVienCurrent = GetCurrentUserId();
                if (maNhanVienCurrent <= 0)
                    return Json(new { success = true, data = new string[] { } });

                var lichSu = await _context.ChamCong
                    .Where(c => c.MaNhanVien == maNhanVienCurrent)
                    .OrderByDescending(c => c.NgayLamViec)
                    .Take(7) // Lấy 7 ngày gần nhất
                    .Select(c => new {
                        Ngay = c.NgayLamViec.ToString("dd/MM/yyyy"),
                        GioVao = c.GioVao.HasValue ? c.GioVao.Value.ToString(@"hh\:mm") : "--:--",
                        GioRa = c.GioRa.HasValue ? c.GioRa.Value.ToString(@"hh\:mm") : "--:--"
                    })
                    .ToListAsync();

                return Json(new { success = true, data = lichSu });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}