using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Web_quan_ly_nhan_su.Context;
using Web_quan_ly_nhan_su.Models;
using System.Security.Cryptography;
using System.Text;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

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
                var nhanVien = _context.NhanVien
                    .Include(nv => nv.NhanVienVaiTro)
                    .ThenInclude(nvvt => nvvt.VaiTro)
                    .FirstOrDefault(nv => nv.Email == email);

                if (nhanVien != null)
                {
                    string hashedInputPassword = ComputeSha256Hash(password);

                    if (nhanVien.MatKhauHash == hashedInputPassword)
                    {
                        var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, nhanVien.HoTen),
                    new Claim(ClaimTypes.Email, nhanVien.Email),
                    new Claim(ClaimTypes.NameIdentifier, nhanVien.MaNhanVien.ToString())
                };

                        // Biến tạm để lưu vai trò đầu tiên tìm thấy phục vụ Session công việc nhanh
                        string primaryRole = "";

                        if (nhanVien.NhanVienVaiTro != null)
                        {
                            foreach (var nvvt in nhanVien.NhanVienVaiTro)
                            {
                                if (nvvt.VaiTro != null && !string.IsNullOrEmpty(nvvt.VaiTro.MaCode))
                                {
                                    claims.Add(new Claim(ClaimTypes.Role, nvvt.VaiTro.MaCode));

                                    if (string.IsNullOrEmpty(primaryRole))
                                    {
                                        primaryRole = nvvt.VaiTro.MaCode; // Lấy mã Code vai trò (ví dụ: ADMIN hoặc HR)
                                    }
                                }
                            }
                        }

                        // --- BỔ SUNG LƯU SESSION ĐỒNG BỘ CHO HỆ THỐNG CHẤM CÔNG ---
                        HttpContext.Session.SetInt32("MaNhanVien", nhanVien.MaNhanVien);
                        if (!string.IsNullOrEmpty(primaryRole))
                        {
                            HttpContext.Session.SetString("Role", primaryRole);
                            HttpContext.Session.SetString("VaiTro", primaryRole);
                        }
                        // ---------------------------------------------------------

                        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                        var authProperties = new AuthenticationProperties
                        {
                            IsPersistent = true,
                            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
                        };

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
            if (!User.Identity.IsAuthenticated) return RedirectToAction("Login", "Account");
            return View("~/Views/Home/TongQuat.cshtml");
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }
    }
}