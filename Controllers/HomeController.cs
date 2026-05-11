using Microsoft.AspNetCore.Mvc;

namespace Web_quan_ly_nhan_su.Controllers
{
    public class HomeController : Controller
    {
        // 1. KHI VỪA MỞ WEB (Chạy vào Index mặc định), TỰ ĐỘNG CHUYỂN SANG TRANG TỔNG QUAN
        public IActionResult Index()
        {
            return RedirectToAction("Tổng quan");
        }

        // 2. Trỏ đến Views/Home/TongQuat.cshtml
        public IActionResult TongQuat()
        {
            ViewData["PageHeader"] = "Tổng quan"; // Đổi tên Tiêu đề Header
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
    }
}