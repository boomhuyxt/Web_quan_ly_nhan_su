using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // Đảm bảo sử dụng được .Include() và .FirstOrDefaultAsync()
using System;
using System.Collections.Generic;
using System.IO; // Sử dụng MemoryStream để kết xuất dữ liệu file Excel
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks; // Bắt buộc phải có để sử dụng cơ chế xử lý dữ liệu bất đồng bộ Task
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

        // ====================================================================
        // 6.5. TRANG QUẢN LÝ XEM & SỬA CHẤM CÔNG (BỔ SUNG CHO ADMIN/HR)
        // ====================================================================
        [HttpGet]
        public IActionResult QuanLyChamCong(int? maNhanVien, string? tenNhanVien, int? thang, int? nam)
        {
            // 1. Kiểm tra quyền Admin bảo mật qua Database tương tự trang CaiDat
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
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập khu vực Quản lý chấm công!";
                return RedirectToAction("TongQuat", "Home");
            }

            // 2. Thiết lập bộ lọc mặc định nếu để trống
            int selectedThang = thang ?? DateTime.Now.Month;
            int selectedNam = nam ?? DateTime.Now.Year;

            ViewBag.SelectedNhanVien = maNhanVien;
            ViewBag.TenNhanVien = tenNhanVien; // Giữ trạng thái chuỗi tìm kiếm tương đối trên UI
            ViewBag.Thang = selectedThang;
            ViewBag.Nam = selectedNam;

            // 3. Lấy danh sách toàn bộ nhân viên để nạp vào thẻ Select lọc dữ liệu
            var dsNhanVien = _context.NhanVien.OrderBy(n => n.HoTen).ToList();

            // 4. Lấy danh sách dữ liệu chấm công theo Tháng và Năm được chọn
            var query = _context.ChamCong.Include(c => c.NhanVien).AsQueryable();

            query = query.Where(c => c.NgayLamViec.Month == selectedThang && c.NgayLamViec.Year == selectedNam);

            // TÌM KIẾM TUYỆT ĐỐI THEO MÃ NHÂN VIÊN (NẾU CHỌN TRÊN DROPDOWN)
            if (maNhanVien.HasValue && maNhanVien > 0)
            {
                query = query.Where(c => c.MaNhanVien == maNhanVien.Value);
            }

            // TÌM KIẾM TƯƠNG ĐỐI THEO TÊN NHÂN VIÊN (TÌM MỜ GẦN ĐÚNG CHUỖI KÝ TỰ)
            if (!string.IsNullOrEmpty(tenNhanVien))
            {
                string keySearch = tenNhanVien.Trim().ToLower();
                query = query.Where(c => c.NhanVien.HoTen.ToLower().Contains(keySearch));
            }

            ViewBag.DsChamCong = query.OrderByDescending(c => c.NgayLamViec).ToList();
            ViewData["PageHeader"] = "Quản lý chấm công";

            // Trả danh sách nhân viên làm Model chính cho View
            return View(dsNhanVien);
        }

       
        public class CapNhatChamCongRequest
        {
            public int MaChamCong { get; set; }
            public int MaNhanVien { get; set; }
            public string NgayLamViec { get; set; }
            public string? GioVao { get; set; }
            public string? GioRa { get; set; }
            public bool IsChamCong { get; set; } // true: Có đi làm/Có giờ chấm, false: Chưa chấm công (Xóa ngày đó)
        }

        // ====================================================================
        // 6.6. API CẬP NHẬT TRẠNG THÁI / GIỜ CHẤM CÔNG (DÀNH CHO ADMIN)
        // ====================================================================
        [HttpPost]
        public async Task<IActionResult> CapNhatChamCongAdmin([FromBody] CapNhatChamCongRequest request)
        {
            // 1. Kiểm tra quyền Admin bảo mật qua Database tương tự trang CaiDat
            var currentUserIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserIdString) || !int.TryParse(currentUserIdString, out int currentUserId))
            {
                return Json(new { success = false, message = "Phiên đăng nhập hết hạn, vui lòng đăng nhập lại." });
            }

            var isAdmin = _context.NhanVienVaiTro
                                  .Include(nvvt => nvvt.VaiTro)
                                  .Any(nvvt => nvvt.MaNhanVien == currentUserId &&
                                               (nvvt.VaiTro.MaCode == "ADMIN" ||
                                                nvvt.VaiTro.TenVaiTro == "Admin" ||
                                                nvvt.VaiTro.TenVaiTro == "ADMIN"));

            if (!isAdmin)
            {
                return Json(new { success = false, message = "Bạn không có quyền thực hiện hành động này!" });
            }

            try
            {
                if (request == null || request.MaNhanVien <= 0)
                    return Json(new { success = false, message = "Dữ liệu gửi lên không hợp lệ." });

                DateTime ngayXuly = DateTime.Parse(request.NgayLamViec).Date;

                // Tìm bản ghi chấm công đã tồn tại của nhân viên trong ngày đó chưa
                var chamCong = await _context.ChamCong
                    .FirstOrDefaultAsync(c => (request.MaChamCong > 0 && c.MaChamCong == request.MaChamCong)
                                           || (c.MaNhanVien == request.MaNhanVien && c.NgayLamViec == ngayXuly));

                if (!request.IsChamCong)
                {
                    // Tình huống: Admin chọn "Chưa chấm công" -> Tiến hành xóa hẳn bản ghi chấm công của ngày này
                    if (chamCong != null)
                    {
                        _context.ChamCong.Remove(chamCong);
                        await _context.SaveChangesAsync();
                    }
                    return Json(new { success = true, message = "Đã cập nhật trạng thái thành: Chưa chấm công" });
                }
                else
                {
                    // Tình huống: Admin chọn "Đã chấm công" hoặc hiệu chỉnh giờ làm việc cụ thể
                    TimeSpan? gioVaoParsed = !string.IsNullOrEmpty(request.GioVao) ? TimeSpan.Parse(request.GioVao) : new TimeSpan(8, 0, 0);
                    TimeSpan? gioRaParsed = !string.IsNullOrEmpty(request.GioRa) ? TimeSpan.Parse(request.GioRa) : new TimeSpan(17, 0, 0);

                    if (chamCong == null)
                    {
                        // Nếu chưa từng tồn tại bản ghi chấm công ngày này, tiến hành tạo mới trực tiếp
                        var newCc = new ChamCong
                        {
                            MaNhanVien = request.MaNhanVien,
                            NgayLamViec = ngayXuly,
                            GioVao = gioVaoParsed,
                            GioRa = gioRaParsed
                        };
                        _context.ChamCong.Add(newCc);
                    }
                    else
                    {
                        // Nếu đã có dữ liệu, cập nhật lại khung giờ mới do Admin chỉnh sửa
                        chamCong.GioVao = gioVaoParsed;
                        chamCong.GioRa = gioRaParsed;
                        _context.ChamCong.Update(chamCong);
                    }

                    await _context.SaveChangesAsync();
                    return Json(new { success = true, message = "Cập nhật dữ liệu giờ chấm công thành công!" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
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