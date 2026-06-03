using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Web_quan_ly_nhan_su.Context;
using Web_quan_ly_nhan_su.Models;
using Supabase;
using System.Security.Claims;

namespace Web_quan_ly_nhan_su.Controllers
{
    public class NhanVienController : Controller
    {
        private readonly AppDbContext _context;
        private readonly string _supabaseUrl = "https://dwdvizkleazjodyfbovl.supabase.co";
        private readonly string _supabaseKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImR3ZHZpemtsZWF6am9keWZib3ZsIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NzY5NTMxNzcsImV4cCI6MjA5MjUyOTE3N30.Kf-Rp5oup1xGm-l8yjZfzY_3kGsOMotQCrqKJx6l88w";

        public NhanVienController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> ThongTinUser()
        {
            var userEmail = User.FindFirstValue(ClaimTypes.Email);

            if (string.IsNullOrEmpty(userEmail))
            {
                return RedirectToAction("Login", "Account");
            }

            var nhanVien = await _context.NhanVien
                .FirstOrDefaultAsync(nv => nv.Email == userEmail);

            if (nhanVien == null)
            {
                return NotFound("Không tìm thấy thông tin nhân viên với Email này.");
            }

            return View("~/Views/Home/thongtinUser.cshtml", nhanVien);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ThongTinUser(NhanVien nvUpdate, IFormFile uploadAnh)
        {
            var userEmail = User.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrEmpty(userEmail)) return Challenge();

            try
            {
                var currentNV = await _context.NhanVien
                    .FirstOrDefaultAsync(m => m.Email == userEmail);

                if (currentNV == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy dữ liệu nhân viên để cập nhật!";
                    return RedirectToAction("ThongTinUser");
                }

                // 1. XỬ LÝ UPLOAD ẢNH LÊN SUPABASE
                if (uploadAnh != null && uploadAnh.Length > 0)
                {
                    var options = new SupabaseOptions { AutoConnectRealtime = true };
                    var supabase = new Supabase.Client(_supabaseUrl, _supabaseKey, options);
                    await supabase.InitializeAsync();

                    using var ms = new MemoryStream();
                    await uploadAnh.CopyToAsync(ms);
                    var fileBytes = ms.ToArray();

                    string fileName = $"{Guid.NewGuid()}{Path.GetExtension(uploadAnh.FileName)}";
                    await supabase.Storage.From("avatars").Upload(fileBytes, fileName);

                    currentNV.AnhDaiDien = supabase.Storage.From("avatars").GetPublicUrl(fileName);
                }

                // 2. CẬP NHẬT TRỰC TIẾP VÀO currentNV
                currentNV.HoTen = nvUpdate.HoTen;
                currentNV.SoDienThoai = nvUpdate.SoDienThoai;
                currentNV.GioiTinh = nvUpdate.GioiTinh;
                currentNV.DiaChi = nvUpdate.DiaChi;

                if (nvUpdate.NgaySinh.HasValue)
                {
                    currentNV.NgaySinh = DateTime.SpecifyKind(nvUpdate.NgaySinh.Value, DateTimeKind.Utc);
                }
                else
                {
                    currentNV.NgaySinh = null;
                }

                // 3. LƯU VÀO DATABASE
                await _context.SaveChangesAsync();

                // 4. THÔNG BÁO THÀNH CÔNG
                TempData["SuccessMessage"] = "Cập nhật thông tin cá nhân thành công!";
                return RedirectToAction("ThongTinUser");
            }
            catch (Exception ex)
            {
                // BẮT LỖI VÀ LẤY LỖI CHI TIẾT (INNER EXCEPTION) IN RA MÀN HÌNH
                string detailError = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                TempData["ErrorMessage"] = "Cập nhật thất bại: " + detailError;
                return RedirectToAction("ThongTinUser");
            }
        }
    }
}