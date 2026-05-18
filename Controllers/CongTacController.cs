using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using Web_quan_ly_nhan_su.Context;
using Web_quan_ly_nhan_su.Models;

namespace Web_quan_ly_nhan_su.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    // [Authorize] // Mở comment dòng này ra nếu muốn bắt buộc đăng nhập
    public class CongTacController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CongTacController(AppDbContext context)
        {
            _context = context;
        }

        // ====================================================================
        // 1. API: THÊM LỊCH CÔNG TÁC
        // ====================================================================
        [HttpPost("them")]
        public async Task<IActionResult> ThemCongTac([FromBody] ThemCongTacRequest request)
        {
            try
            {
                // Validate cơ bản
                if (request.MaNhanVien <= 0)
                    return BadRequest(new { success = false, message = "Vui lòng chọn nhân viên." });

                if (request.NgayBatDau > request.NgayKetThuc)
                    return BadRequest(new { success = false, message = "Ngày bắt đầu không được lớn hơn ngày kết thúc." });

                if (string.IsNullOrEmpty(request.DiaDiem) || string.IsNullOrEmpty(request.NoiDungCongViec))
                    return BadRequest(new { success = false, message = "Vui lòng nhập địa điểm và nội dung công tác." });

                // Kiểm tra nhân viên có tồn tại không
                var nhanVien = await _context.NhanVien.FindAsync(request.MaNhanVien);
                if (nhanVien == null)
                    return NotFound(new { success = false, message = "Nhân viên không tồn tại trong hệ thống." });

                // Tạo Entity mới để lưu vào Database
                var lichMoi = new LichCongTac
                {
                    MaNhanVien = request.MaNhanVien,
                    NgayBatDau = request.NgayBatDau,
                    NgayKetThuc = request.NgayKetThuc,
                    DiaDiem = request.DiaDiem,
                    NoiDungCongViec = request.NoiDungCongViec,
                    FileDinhKemUrl = request.FileDinhKemUrl, // Lưu link file Supabase (nếu có)
                    TrangThai = "Sắp tới", // Mặc định khi mới tạo là Sắp tới
                    NgayTao = DateTime.Now
                };

                _context.LichCongTacs.Add(lichMoi);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Đã xếp lịch công tác thành công!" });
            }
            catch (Exception ex)
            {
                var errorMsg = ex.InnerException?.Message ?? ex.Message;
                return StatusCode(500, new { success = false, message = "Lỗi Database: " + errorMsg });
            }
        }

        // ====================================================================
        // 2. API: LẤY DANH SÁCH LỊCH CÔNG TÁC (ĐỂ HIỂN THỊ LÊN GIAO DIỆN)
        // ====================================================================
        [HttpGet("danh-sach")]
        public async Task<IActionResult> GetDanhSach()
        {
            try
            {
                // Lấy danh sách lịch công tác, JOIN với bảng NhanVien để lấy tên và Avatar
                var danhSach = await _context.LichCongTacs
                    .Include(x => x.NhanVien)
                    .OrderByDescending(x => x.NgayTao)
                    .Select(x => new
                    {
                        x.Id,
                        x.DiaDiem,
                        x.NoiDungCongViec,
                        x.TrangThai,
                        x.FileDinhKemUrl,
                        NgayBatDau = x.NgayBatDau.ToString("dd/MM/yyyy"),
                        NgayKetThuc = x.NgayKetThuc.ToString("dd/MM/yyyy"),
                        TenNhanVien = x.NhanVien.HoTen,
                        AnhDaiDien = x.NhanVien.AnhDaiDien ?? "/images/avatar_default.jpg"
                    })
                    .ToListAsync();

                return Ok(new { success = true, data = danhSach });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi tải dữ liệu: " + ex.Message });
            }
        }

        // ====================================================================
        // 3. API: LẤY LỊCH CÔNG TÁC CỦA RIÊNG TÔI (CHO TRANG CÁ NHÂN)
        // ====================================================================
        [HttpGet("danh-sach-cua-toi")]
        public async Task<IActionResult> GetDanhSachCuaToi()
        {
            try
            {
                // 👉 BƯỚC A: Lấy ID của nhân viên ĐANG ĐĂNG NHẬP
                // (Tùy vào cách ông viết hàm Login, thường sẽ lưu ID vào Session hoặc Cookie Claims)

                // Mẫu 1: Thử lấy từ Session (Nếu lúc Login ông dùng HttpContext.Session)
                int currentUserId = HttpContext.Session.GetInt32("MaNhanVien") ?? 0;

                // Mẫu 2: Thử lấy từ Cookie Authentication (Nếu lúc Login ông dùng User.Claims)
                if (currentUserId == 0)
                {
                    var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "MaNhanVien")?.Value
                                   ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                    int.TryParse(userIdClaim, out currentUserId);
                }

                // Nếu hệ thống không tìm thấy ai đang đăng nhập -> Trả về mảng rỗng (Trống)
                if (currentUserId <= 0)
                {
                    return Ok(new { success = true, data = new string[] { } });
                }

                // 👉 BƯỚC B: Lọc Database CHỈ LẤY đúng lịch của currentUserId
                var danhSach = await _context.LichCongTacs
                    .Include(x => x.NhanVien)
                    .Where(x => x.MaNhanVien == currentUserId) // <--- ĐÂY LÀ DÒNG BẢO MẬT QUAN TRỌNG NHẤT
                    .OrderByDescending(x => x.NgayTao)
                    .Select(x => new
                    {
                        x.Id,
                        x.DiaDiem,
                        x.NoiDungCongViec,
                        x.TrangThai,
                        x.FileDinhKemUrl,
                        NgayBatDau = x.NgayBatDau.ToString("HH:mm dd/MM/yyyy"),
                        NgayKetThuc = x.NgayKetThuc.ToString("HH:mm dd/MM/yyyy"),
                        TenNhanVien = x.NhanVien.HoTen,
                        AnhDaiDien = x.NhanVien.AnhDaiDien ?? "/images/avatar_default.jpg"
                    })
                    .ToListAsync();

                return Ok(new { success = true, data = danhSach });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi tải dữ liệu: " + ex.Message });
            }
        }
    }

    // ====================================================================
    // DTO: CLASS ĐỂ HỨNG DỮ LIỆU TỪ GIAO DIỆN JAVASCRIPT GỬI LÊN
    // ====================================================================
    public class ThemCongTacRequest
    {
        public int MaNhanVien { get; set; }
        public DateTime NgayBatDau { get; set; }
        public DateTime NgayKetThuc { get; set; }
        public string DiaDiem { get; set; }
        public string NoiDungCongViec { get; set; }
        public string? FileDinhKemUrl { get; set; } // Link file tài liệu nếu có
    }
}