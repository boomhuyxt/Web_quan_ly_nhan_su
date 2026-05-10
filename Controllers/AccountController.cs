using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Web_quan_ly_nhan_su.Context;
using Web_quan_ly_nhan_su.Models;
using System.Security.Cryptography;
using System.Text;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

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
        public IActionResult Login() => View();

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            if (!string.IsNullOrEmpty(email) && !string.IsNullOrEmpty(password))
            {
                // 1. Tìm nhân viên theo Email 
                var nhanVien = _context.NhanVien.FirstOrDefault(nv => nv.Email == email);

                if (nhanVien != null)
                {
                    // 2. Kiểm tra mật khẩu 
                    string hashedInputPassword = ComputeSha256Hash(password);

                    if (nhanVien.MatKhauHash == hashedInputPassword)
                    {
                        // 3. TẠO DANH SÁCH CLAIMS 
                        var claims = new List<Claim>
                        {
                            new Claim(ClaimTypes.Name, nhanVien.HoTen),
                            // QUAN TRỌNG: Lưu Email để NhanVienController lấy thông tin cập nhật
                            new Claim(ClaimTypes.Email, nhanVien.Email), 
                            // Lưu ID để hỗ trợ các chức năng khác nếu cần
                            new Claim(ClaimTypes.NameIdentifier, nhanVien.MaNhanVien.ToString())
                        };

                        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                        var authProperties = new AuthenticationProperties
                        {
                            IsPersistent = true, // Ghi nhớ đăng nhập
                            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
                        };

                        // 4. Đăng nhập bằng Cookie 
                        await HttpContext.SignInAsync(
                            CookieAuthenticationDefaults.AuthenticationScheme,
                            new ClaimsPrincipal(claimsIdentity),
                            authProperties);

                        return RedirectToAction("TongQuat", "Account");
                    }
                }
                ViewBag.ErrorMessage = "Email hoặc mật khẩu không chính xác.";
            }
            else
            {
                ViewBag.ErrorMessage = "Vui lòng nhập đầy đủ email và mật khẩu.";
            }
            return View();
        }

        [HttpGet]
        public IActionResult TongQuat()
        {
            // Kiểm tra quyền truy cập thông qua Identity 
            if (!User.Identity.IsAuthenticated) return RedirectToAction("Login", "Account");

           // Chỉ định đường dẫn tới thư mục Home 
            return View("~/Views/Home/TongQuat.cshtml");
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
           // Đăng xuất và xóa Cookie 
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }
    }
}