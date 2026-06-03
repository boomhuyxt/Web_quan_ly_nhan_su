using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Web_quan_ly_nhan_su.Context;

namespace Web_quan_ly_nhan_su.Controllers
{
    [Authorize(Roles = "ADMIN")]
    public class QuanTriNghiPhepController : Controller
    {
        private readonly AppDbContext _context;

        private readonly string _supabaseUrl = "https://dwdvizkleazjodyfbovl.supabase.co";
        private readonly string _supabaseKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImR3ZHZpemtsZWF6am9keWZib3ZsIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NzY5NTMxNzcsImV4cCI6MjA5MjUyOTE3N30.Kf-Rp5oup1xGm-l8yjZfzY_3kGsOMotQCrqKJx6l88w";


        // Khởi tạo Database Context
        public QuanTriNghiPhepController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Index()
        {
            // Lấy MaNhanVien từ Identity Cookie
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int maNhanVienDangNhap))
            {
                return RedirectToAction("Login", "Account");
            }

            // Lấy danh sách đơn nghỉ phép của riêng nhân viên này [cite: 26, 27]
            var danhSachDon = _context.NghiPhep
                                      .Where(n => n.MaNhanVien == maNhanVienDangNhap)
                                      .OrderByDescending(n => n.NgayBatDau)
                                      .ToList();

            return View("~/Views/Home/QuanTriNghiPhepController.cs", danhSachDon);
        }

        // API Xử lý Phê duyệt / Từ chối đơn nghỉ phép
        [HttpPost]
        public async Task<IActionResult> PheDuyet(int id, string trangThai)
        {
            try
            {
                // 1. Tìm đơn nghỉ phép trong Cơ sở dữ liệu
                var donNghiPhep = await _context.NghiPhep.FindAsync(id);

                if (donNghiPhep == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy đơn xin nghỉ phép này." });
                }

                // 2. Kiểm tra xem trạng thái truyền lên có hợp lệ không
                if (trangThai == "Đã duyệt" || trangThai == "Từ chối")
                {
                    // 3. Cập nhật trạng thái và Lưu vào CSDL
                    donNghiPhep.TrangThai = trangThai;
                    await _context.SaveChangesAsync();

                    return Json(new { success = true, message = $"Đã {trangThai.ToLower()} đơn nghỉ phép thành công!" });
                }

                return Json(new { success = false, message = "Trạng thái phê duyệt không hợp lệ." });
            }
            catch (Exception ex)
            {
                // Bắt lỗi nếu có trục trặc về kết nối Database
                return Json(new { success = false, message = "Đã xảy ra lỗi hệ thống: " + ex.Message });
            }
        }
    }
}