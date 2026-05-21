using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;
using Web_quan_ly_nhan_su.Context;
using Web_quan_ly_nhan_su.Models;

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
        // 👉 ĐÃ SỬA: Đồng bộ Role thành "Admin" để tránh lỗi không khớp ký tự viết hoa chữ thường
        [HttpGet]
        public IActionResult CaiDat()
        {
            var currentUserIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserIdString) || !int.TryParse(currentUserIdString, out int currentUserId))
            {
                return RedirectToAction("Login", "Account");
            }

            var isAdmin = _context.NhanVienVaiTro
                                  .Include(nvvt => nvvt.VaiTro)
                                  .Any(nvvt => nvvt.MaNhanVien == currentUserId &&
                                               (nvvt.VaiTro.MaCode == "ADMIN" ||
                                                nvvt.VaiTro.TenVaiTro == "Admin" ||
                                                nvvt.VaiTro.TenVaiTro == "ADMIN"));

            if (!isAdmin)
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập khu vực quản trị!";
                return RedirectToAction("TongQuat", "Home");
            }

            ViewData["PageHeader"] = "Cài đặt & Quản trị";
            var danhSachDon = _context.NghiPhep
                                      .Include(n => n.NhanVien)
                                      .OrderByDescending(n => n.TrangThai == "Chờ duyệt")
                                      .ThenByDescending(n => n.NgayBatDau)
                                      .ToList();

            return View(danhSachDon);
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
            return View(danhSachDon);
        }

        // ====================================================================
        // HIỂN THỊ TẤT CẢ LƯƠNG TRONG DATA KÈM HÌNH ẢNH + TÊN NHÂN VIÊN
        // ====================================================================
        public IActionResult Luong()
        {
            var danhSachLuong = _context.Luong
                                        .Include(l => l.NhanVien)
                                        .OrderByDescending(l => l.Nam)
                                        .ThenByDescending(l => l.Thang)
                                        .ToList();

            ViewData["PageHeader"] = "Quản lý Bảng Lương";
            return View(danhSachLuong);
        }

        public IActionResult LichCongTac()
        {
            return View();
        }

        // ====================================================================
        // TRANG XEM LƯƠNG CỦA CÁ NHÂN (DÀNH CHO NHÂN VIÊN)
        // ====================================================================
        public IActionResult PhieuLuongCaNhan()
        {
            // Lấy ID người đang đăng nhập
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int maNhanVienDangNhap))
            {
                return RedirectToAction("Login", "Account");
            }

            // Chỉ lấy bảng lương của người đang đăng nhập
            var danhSachLuong = _context.Luong
                                        .Include(l => l.NhanVien)
                                        .Where(l => l.MaNhanVien == maNhanVienDangNhap)
                                        .OrderByDescending(l => l.Nam)
                                        .ThenByDescending(l => l.Thang)
                                        .ToList();

            ViewData["PageHeader"] = "Phiếu lương cá nhân";
            return View(danhSachLuong);
        }

        [HttpGet]
        // 👉 BƯỚC 1: Bỏ thuộc tính [Authorize(Roles = "Admin")] để tự xử lý logic bằng mã lệnh bên dưới
        public IActionResult ThongTinChiTiet(int id)
        {
            // 1. Lấy ID của nhân viên đang thao tác đăng nhập hiện tại trên hệ thống
            var currentUserIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserIdString) || !int.TryParse(currentUserIdString, out int currentUserId))
            {
                // Nếu phiên đăng nhập hết hạn hoặc chưa đăng nhập, đá ra trang Login
                return RedirectToAction("Login", "Account");
            }

            // 2. Tìm kiếm trực tiếp trong Database xem nhân viên này có sở hữu mã quyền Quản trị hay không
            // (Kiểm tra cả trường hợp Database lưu là "ADMIN" hoặc "Admin")
            var isAdmin = _context.NhanVienVaiTro
                                  .Include(nvvt => nvvt.VaiTro)
                                  .Any(nvvt => nvvt.MaNhanVien == currentUserId &&
                                               (nvvt.VaiTro.MaCode == "ADMIN" ||
                                                nvvt.VaiTro.TenVaiTro == "Admin" ||
                                                nvvt.VaiTro.TenVaiTro == "ADMIN"));

            // 3. Nếu KHÔNG PHẢI Admin, chặn đứng hành động và đẩy ngược về danh sách kèm thông báo lỗi
            if (!isAdmin)
            {
                
                return RedirectToAction("NhanVien", "Home");
            }

            // 4. Nếu ĐÚNG LÀ Admin, tiến hành lấy thông tin nhân viên cần xem như bình thường
            var nhanVien = _context.NhanVien
                                   .Include(n => n.PhongBan)
                                   .FirstOrDefault(n => n.MaNhanVien == id);

            if (nhanVien == null) return NotFound();

            ViewData["PageHeader"] = "Chi tiết nhân viên";
            return View(nhanVien);
        }
    }
}