using Microsoft.AspNetCore.Mvc;
using Supabase;
using System;
using System.IO;
using System.Linq;
using System.Security.Claims; // Bắt buộc thêm thư viện này để đọc Claims từ Cookie
using System.Threading.Tasks;
using Web_quan_ly_nhan_su.Context;
using Web_quan_ly_nhan_su.Models;
using Microsoft.EntityFrameworkCore;

namespace Web_quan_ly_nhan_su.Controllers
{
    public class NghiPhepController : Controller
    {
        private readonly AppDbContext _context;
        private readonly Client _supabaseClient;

        private readonly string _supabaseUrl = "https://dwdvizkleazjodyfbovl.supabase.co";
        private readonly string _supabaseKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImR3ZHZpemtsZWF6am9keWZib3ZsIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NzY5NTMxNzcsImV4cCI6MjA5MjUyOTE3N30.Kf-Rp5oup1xGm-l8yjZfzY_3kGsOMotQCrqKJx6l88w";

        public NghiPhepController(AppDbContext context)
        {
            _context = context;
            var options = new SupabaseOptions { AutoConnectRealtime = true };
            _supabaseClient = new Client(_supabaseUrl, _supabaseKey, options);
        }

        // 1. HÀM NÀY HIỂN THỊ GIAO DIỆN VÀ DANH SÁCH ĐƠN
        [HttpGet]
        public IActionResult Index()
        {
            // Lấy MaNhanVien từ Identity Cookie
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int maNhanVienDangNhap))
            {
                return RedirectToAction("Login", "Account");
            }

            // Lấy danh sách đơn nghỉ phép của riêng nhân viên này [cite: 26, 27]
            var danhSachDon = _context.NghiPhep
                                      .Where(n => n.MaNhanVien == maNhanVienDangNhap)
                                      .OrderByDescending(n => n.NgayBatDau)
                                      .ToList();

            return View("~/Views/Home/NghiPhep.cshtml", danhSachDon);
        }

        // 2. HÀM NÀY XỬ LÝ KHI NGƯỜI DÙNG BẤM "GỬI YÊU CẦU" TRÊN MODAL
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TaoMoi(NghiPhep model)
        {
            // Kiểm tra đăng nhập
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }

            // Lấy lại MaNhanVien từ Cookie
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int maNhanVienDangNhap))
            {
                return RedirectToAction("Login", "Account");
            }

            // BẮT BUỘC: Xóa validate cho MaNhanVien vì ta gán nó từ thông tin đăng nhập
            ModelState.Remove("MaNhanVien");
            ModelState.Remove("NhanVien");

            if (ModelState.IsValid)
            {
                try
                {
                    if (model.FileMinhChung != null && model.FileMinhChung.Length > 0)
                    {
                        using var memoryStream = new MemoryStream();
                        await model.FileMinhChung.CopyToAsync(memoryStream);
                        var fileBytes = memoryStream.ToArray();

                        var fileExtension = Path.GetExtension(model.FileMinhChung.FileName);
                        var fileName = $"minhchung_{Guid.NewGuid()}{fileExtension}";

                        await _supabaseClient.Storage
                            .From("MinhChung")
                            .Upload(fileBytes, fileName, new Supabase.Storage.FileOptions { ContentType = model.FileMinhChung.ContentType });

                        model.MinhChungUrl = _supabaseClient.Storage.From("MinhChung").GetPublicUrl(fileName);
                    }

                    // GÁN ID LẤY TỪ COOKIE VÀO MODEL
                    model.MaNhanVien = maNhanVienDangNhap;
                    model.TrangThai = "Chờ duyệt";

                    // MỚI THÊM: Ép kiểu DateTime về chuẩn UTC để PostgreSQL chấp nhận
                    model.NgayBatDau = DateTime.SpecifyKind(model.NgayBatDau, DateTimeKind.Utc);
                    model.NgayKetThuc = DateTime.SpecifyKind(model.NgayKetThuc, DateTimeKind.Utc);

                    _context.NghiPhep.Add(model);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Đã gửi đơn xin nghỉ phép thành công!";
                }
                catch (Exception ex)
                {
                    var loiChiTiet = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                    TempData["Error"] = "Lỗi CSDL: " + loiChiTiet;
                }
            }
            else
            {
                var errors = string.Join(" | ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                TempData["Error"] = "Dữ liệu form không hợp lệ: " + errors;
            }

            return RedirectToAction("Index");
        }

        [HttpGet]
        // Bạn có thể thêm phân quyền [Authorize(Roles = "ADMIN")] nếu chỉ muốn Admin xem hết
        public IActionResult TatCaDonNghiPhep()
        {
            // Truy vấn tất cả các đơn nghỉ phép từ Database
            // Sử dụng .Include(n => n.NhanVien) để lấy thông tin họ tên nhân viên gửi đơn
            var tatCaDon = _context.NghiPhep
                                   .Include(n => n.NhanVien)
                                   .OrderByDescending(n => n.Id) // Đơn mới nhất lên đầu
                                   .ToList();

            ViewData["PageHeader"] = "Quản lý toàn bộ đơn nghỉ phép";

            // Trả về View cùng danh sách dữ liệu
            return View(tatCaDon);
        }
    }
}