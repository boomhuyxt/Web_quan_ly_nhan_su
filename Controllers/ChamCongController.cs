using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
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

        // Lớp nhận dữ liệu JSON từ Javascript gửi lên
        public class FaceRequest
        {
            public string ImageBase64 { get; set; }
        }

        // 1. API ĐĂNG KÝ KHUÔN MẶT
        [HttpPost]
        public async Task<IActionResult> DangKyKhuonMat([FromBody] FaceRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.ImageBase64))
                {
                    return Json(new { success = false, message = "Không nhận được dữ liệu ảnh." });
                }

                // Giả lập ID nhân viên đang đăng nhập (Thực tế bạn lấy từ Session/User.Identity)
                int maNhanVienCurrent = 1;

                var nhanVien = await _context.NhanVien.FindAsync(maNhanVienCurrent);
                if (nhanVien == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy thông tin nhân viên." });
                }

                // Lưu chuỗi Base64 (hoặc Vector khuôn mặt nếu đã qua model AI) vào database
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
                int maNhanVienCurrent = 1; // ID nhân viên giả lập
                var nhanVien = await _context.NhanVien.FindAsync(maNhanVienCurrent);

                if (nhanVien == null || string.IsNullOrEmpty(nhanVien.FaceVector))
                {
                    return Json(new { success = false, message = "Bạn chưa đăng ký khuôn mặt!" });
                }

                // [Phần AI] - So sánh request.ImageBase64 với nhanVien.FaceVector
                // Tạm thời bỏ qua logic AI phức tạp, mặc định cho pass để test luồng:
                bool isMatch = true;

                if (!isMatch)
                {
                    return Json(new { success = false, message = "Khuôn mặt không khớp!" });
                }

                // --- FIX LỖI POSTGRESQL UTC TẠI ĐÂY ---
                // Lấy thời gian hiện tại theo múi giờ Việt Nam (UTC + 7)
                DateTime vietnamTime = DateTime.UtcNow.AddHours(7);

                // Ép định dạng Kind thành UTC để PostgreSQL chấp nhận lưu xuống DB
                DateTime homNay = DateTime.SpecifyKind(vietnamTime.Date, DateTimeKind.Utc);

                // Giờ, phút, giây hiện tại
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
    }
}