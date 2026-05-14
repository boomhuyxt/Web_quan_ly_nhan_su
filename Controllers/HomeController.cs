using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;
using Web_quan_ly_nhan_su.Context;
using Web_quan_ly_nhan_su.Models;

namespace Web_quan_ly_nhan_su.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;

        // BẮT BUỘC: Phải có hàm Constructor này để khởi tạo _context
        public HomeController(AppDbContext context)
        {
            _context = context;
        }

        // 1. KHI VỪA MỞ WEB, TỰ ĐỘNG CHUYỂN SANG TRANG TỔNG QUAN
        public IActionResult Index()
        {
            return RedirectToAction("TongQuat");
        }

        // 2. Trỏ đến Views/Home/TongQuat.cshtml
        public IActionResult TongQuat()
        {
            ViewData["PageHeader"] = "Tổng quan";
            return View();
        }

        // 3. Trỏ đến Views/Home/NhanVien.cshtml
        public IActionResult NhanVien()
        {
            ViewData["PageHeader"] = "Nhân viên";
            return View();
        }

        // 4. Trỏ đến Views/Home/ChamCong.cshtml
        public IActionResult ChamCong()
        {
            ViewData["PageHeader"] = "Chấm công";
            return View();
        }

        // 5. Trỏ đến Views/Home/Chat.cshtml
        public IActionResult Chat()
        {
            ViewData["PageHeader"] = "Trò chuyện";
            return View();
        }

        // 6. Trỏ đến Views/Home/CaiDat.cshtml
        [HttpGet]
        [Authorize(Roles = "ADMIN")]
        public IActionResult CaiDat()
        {
            ViewData["PageHeader"] = "Cài đặt & Quản trị";

            // TÌM LỖI LÀ Ở ĐÂY: Truy vấn toàn bộ Đơn xin nghỉ phép kèm thông tin Nhân viên
            var danhSachDon = _context.NghiPhep
                                      .Include(n => n.NhanVien) // Lấy thông tin họ tên, avatar
                                      .OrderByDescending(n => n.TrangThai == "Chờ duyệt") // Đơn chờ duyệt ưu tiên lên đầu
                                      .ThenByDescending(n => n.NgayBatDau) // Sau đó sắp xếp theo ngày
                                      .ToList();

            // Truyền danh sách này sang View để giao diện có data mà render (thẻ @foreach)
            return View(danhSachDon);
        }

        // 7. Trỏ đến Views/Home/ThongTinUser.cshtml
        public IActionResult ThongTinUser()
        {
            ViewData["PageHeader"] = "Thông tin cá nhân";
            return View();
        }

        // 8. Xử lý logic hiển thị danh sách Đơn Nghỉ Phép của cá nhân
        public IActionResult NghiPhep()
        {
            // Lấy MaNhanVien từ Identity Cookie (Sử dụng Claims an toàn) 
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int maNhanVienDangNhap))
            {
                // Nếu chưa đăng nhập, chuyển hướng về Account/Login
                return RedirectToAction("Login", "Account");
            }

            // Truy vấn danh sách đơn nghỉ phép của đúng nhân viên đó từ Database 
            var danhSachDon = _context.NghiPhep
                                      .Where(n => n.MaNhanVien == maNhanVienDangNhap)
                                      .OrderByDescending(n => n.NgayBatDau) // Đơn mới nhất hiện lên đầu
                                      .ToList();

            ViewData["PageHeader"] = "Nghỉ phép";
            return View(danhSachDon);
        }

        public IActionResult Luong()
        {
            // 1. Lấy MaNhanVien từ Identity Cookie an toàn như trang Nghỉ phép
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int maNhanVien))
            {
                return RedirectToAction("Login", "Account");
            }

            // 2. Truy vấn danh sách lương của nhân viên từ model Luong
            var danhSachLuong = _context.Luong
                                        .Where(l => l.MaNhanVien == maNhanVien)
                                        .OrderByDescending(l => l.Nam)
                                        .ThenByDescending(l => l.Thang)
                                        .ToList();

            ViewData["PageHeader"] = "Phiếu lương cá nhân";
            return View(danhSachLuong);
        }
    }
}