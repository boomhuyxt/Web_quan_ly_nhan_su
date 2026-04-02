using Microsoft.AspNetCore.Mvc;

namespace Web_quan_ly_nhan_su.Controllers
{
    public class HomeController : Controller
    {
        // Trỏ đến Views/Home/TongQuat.cshtml
        public IActionResult Index()
        {
            return View("TongQuat");
        }

        // Trỏ đến Views/Home/NhanVien.cshtml
        public IActionResult NhanVien()
        {
            return View("NhanVien");
        }

        // Trỏ đến Views/Home/ChamCong.cshtml
        public IActionResult ChamCong()
        {
            return View("ChamCong");
        }

        // Trỏ đến Views/Home/Chat.cshtml
        public IActionResult Chat()
        {
            return View("Chat");
        }

        // Trỏ đến Views/Home/CaiDat.cshtml
        public IActionResult CaiDat()
        {
            return View("CaiDat");
        }
    }
}