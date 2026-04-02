using Microsoft.AspNetCore.Mvc;
namespace Web_quan_ly_nhan_su.Controllers // Phải đúng với tên project của bạn
{
    public class AccountController : Controller
    {
        public IActionResult Login()
        {
            // Trả về view mà không cần đường dẫn dài dòng nếu cấu trúc thư mục đã chuẩn
            return View();
        }

        [HttpPost]
        public IActionResult Login(string email, string password)
        {
            // Ở đây bạn có thể thêm logic kiểm tra email/password nếu cần
            // Ví dụ: if(email == "admin@atelier.com" && password == "123") ...

            // Chuyển hướng sang Controller: Home, Action: Index (Trang Tổng Quát)
            return RedirectToAction("Index", "Home");
        }
    }
}