using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Linq; // Cần thiết để sử dụng .Where, .OrderByDescending
using Web_quan_ly_nhan_su.Context;
using Web_quan_ly_nhan_su.Models; // Cần thiết để nhận dạng model NghiPhep

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
            // Sửa lại thành "TongQuat" cho khớp với tên Action bên dưới
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
        public IActionResult CaiDat()
        {
            ViewData["PageHeader"] = "Cài đặt";
            return View();
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
            // Truyền danh sách qua View (file Views/Home/NghiPhep.cshtml) 
            return View(danhSachDon);
        }
    }
}