using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Web_quan_ly_nhan_su.Context;
using Web_quan_ly_nhan_su.Models;
using System.Security.Cryptography;
using System.Text;

namespace Web_quan_ly_nhan_su.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;

        public AccountController(AppDbContext context)
        {
            _context = context;
        }

        // Hàm băm mật khẩu SHA-256 
        private string ComputeSha256Hash(string rawData)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // Nhận trực tiếp 2 tham số từ Form HTML gửi lên
        [HttpPost]
        public IActionResult Login(string email, string password)
        {
            // Kiểm tra người dùng đã nhập đủ 2 trường chưa
            if (!string.IsNullOrEmpty(email) && !string.IsNullOrEmpty(password))
            {
                // 1. Tìm nhân viên trong CSDL theo Email 
                var nhanVien = _context.NhanVien.FirstOrDefault(nv => nv.Email == email);

                if (nhanVien != null)
                {
                    // 2. Băm mật khẩu người dùng vừa nhập bằng chuẩn SHA-256
                    string hashedInputPassword = ComputeSha256Hash(password);

                    // 3. So sánh với MatKhauHash đã lưu trong CSDL 
                    if (nhanVien.MatKhauHash == hashedInputPassword)
                    {
                        // Đăng nhập thành công -> Lưu Session
                        HttpContext.Session.SetString("UserEmail", nhanVien.Email);


                        // Chuyển hướng đến trang Tổng Quát
                        return RedirectToAction("TongQuat", "Account");
                    }
                }

                ViewBag.ErrorMessage = "Email hoặc mật khẩu không chính xác.";
            }
            else
            {
                ViewBag.ErrorMessage = "Vui lòng nhập đầy đủ email và mật khẩu.";
            }

            // Trả về lại trang đăng nhập (không cần truyền model về)
            return View();
        }

        [HttpGet]
        public IActionResult TongQuat()
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(userEmail))
            {
                return RedirectToAction("Login", "Account");
            }

            // CHỈ ĐỊNH RÕ ĐƯỜNG DẪN TỚI THƯ MỤC HOME
            return View("~/Views/Home/TongQuat.cshtml");
        }

        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Account");
        }
    }
}