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

            // Truy vấn toàn bộ Đơn xin nghỉ phép kèm thông tin Nhân viên
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

        // ====================================================================
        // 👉 ĐÃ SỬA: HIỂN THỊ TẤT CẢ LƯƠNG TRONG DATA KÈM HÌNH ẢNH + TÊN NHÂN VIÊN
        // ====================================================================
        public IActionResult Luong()
        {
            // 1. Xóa bỏ hoàn toàn phần .Where() để lấy TẤT CẢ dữ liệu lương có trong bảng
            // 2. Thêm lệnh .Include(l => l.NhanVien) để liên kết bảng lấy Hình ảnh, Tên và Email của từng người
            var danhSachLuong = _context.Luong
                                        .Include(l => l.NhanVien)
                                        .OrderByDescending(l => l.Nam)
                                        .ThenByDescending(l => l.Thang)
                                        .ToList();

            ViewData["PageHeader"] = "Quản lý Bảng Lương";
            return View(danhSachLuong);
        }

        public IActionResult LichCongTac()
        {
            return View();
        }
    }
}