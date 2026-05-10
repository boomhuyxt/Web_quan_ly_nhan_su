using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Web_quan_ly_nhan_su.Context;
using Web_quan_ly_nhan_su.Models;
using Supabase;
using System.Security.Claims;
using Microsoft.Extensions.Options; // Nếu bạn đã dùng Models cấu hình Supabase

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

        // GET: Lấy thông tin dựa trên Gmail của người đang đăng nhập
        [HttpGet]
        public async Task<IActionResult> CapNhat()
        {
            // Lấy Email từ Claims của người dùng đã đăng nhập
            var userEmail = User.FindFirstValue(ClaimTypes.Email);

            if (string.IsNullOrEmpty(userEmail))
            {
                return RedirectToAction("Login", "Account");
            }

            // Tìm nhân viên trong DB dựa trên Gmail (Email)
            var nhanVien = await _context.NhanVien
                .FirstOrDefaultAsync(nv => nv.Email == userEmail);

            if (nhanVien == null)
            {
                return NotFound("Không tìm thấy thông tin nhân viên với Gmail này.");
            }

            return View(nhanVien);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CapNhat(NhanVien nvUpdate, IFormFile uploadAnh)
        {
            // Lấy Email từ hệ thống để xác thực
            var userEmail = User.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrEmpty(userEmail)) return Challenge();

            try
            {
                // Tìm dữ liệu gốc dựa trên Email để bảo vệ các trường hệ thống
                var currentNV = await _context.NhanVien.AsNoTracking()
                    .FirstOrDefaultAsync(m => m.Email == userEmail);

                if (currentNV == null) return NotFound();

                // Đảm bảo không ai có thể đổi Email của người khác thông qua Form
                if (currentNV.Email != userEmail) return BadRequest("Hành động không hợp lệ.");

                // --- XỬ LÝ UPLOAD LÊN SUPABASE ---
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

                    nvUpdate.AnhDaiDien = supabase.Storage.From("avatars").GetPublicUrl(fileName);
                }
                else
                {
                    nvUpdate.AnhDaiDien = currentNV.AnhDaiDien;
                }

                // Gán lại các giá trị nhạy cảm (không cho phép sửa qua form)
                nvUpdate.MaNhanVien = currentNV.MaNhanVien; // Giữ nguyên ID gốc
                nvUpdate.Email = currentNV.Email; // Giữ nguyên Email gốc
                nvUpdate.MatKhauHash = currentNV.MatKhauHash;
                nvUpdate.TrangThai = currentNV.TrangThai;
                nvUpdate.MaPhongBan = currentNV.MaPhongBan;
                nvUpdate.FaceVector = currentNV.FaceVector;
                nvUpdate.NgayTao = currentNV.NgayTao;

                _context.Update(nvUpdate);
                await _context.SaveChangesAsync();

                return RedirectToAction("TongQuat", "Account");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Lỗi cập nhật: " + ex.Message);
                return View(nvUpdate);
            }
        }
    }
}