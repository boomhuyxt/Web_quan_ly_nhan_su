using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text;
using Web_quan_ly_nhan_su.Context;
using Web_quan_ly_nhan_su.Models;

namespace Web_quan_ly_nhan_su.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class QuanLyNhanVienController : ControllerBase
    {
        private readonly AppDbContext _context;

        public QuanLyNhanVienController(AppDbContext context)
        {
            _context = context;
        }

        // 1. LẤY DANH SÁCH NHÂN VIÊN (CÓ HỖ TRỢ TÌM KIẾM)
        [HttpGet("danh-sach")]
        public async Task<IActionResult> GetDanhSach([FromQuery] string q = "")
        {
            // Tạo câu truy vấn cơ bản
            var query = _context.NhanVien
                .Include(nv => nv.PhongBan)
                .Include(nv => nv.NhanVienVaiTro).ThenInclude(vt => vt.VaiTro)
                .AsQueryable();

            // Nếu có từ khóa tìm kiếm, áp dụng bộ lọc Tên hoặc Email
            if (!string.IsNullOrWhiteSpace(q))
            {
                string searchLower = q.ToLower();
                query = query.Where(nv => nv.HoTen.ToLower().Contains(searchLower) || nv.Email.ToLower().Contains(searchLower));
            }

            // Thực thi truy vấn và định dạng dữ liệu
            var list = await query
                .OrderByDescending(nv => nv.NgayTao)
                .Select(nv => new {
                    nv.MaNhanVien,
                    nv.HoTen,
                    nv.Email,
                    nv.AnhDaiDien,
                    nv.TrangThai,
                    TenPhongBan = nv.PhongBan != null ? nv.PhongBan.TenPhongBan : "Chưa phân bổ",
                    MaPhongBan = nv.MaPhongBan,
                    VaiTros = nv.NhanVienVaiTro.Select(v => new { v.MaVaiTro, v.VaiTro.TenVaiTro }).ToList()
                })
                .ToListAsync();

            return Ok(new { success = true, data = list });
        }

        // 2. LẤY MASTER DATA (Phòng ban & Vai trò)
        [HttpGet("master-data")]
        public async Task<IActionResult> GetMasterData()
        {
            var phongBans = await _context.PhongBan.Select(p => new { p.MaPhongBan, p.TenPhongBan }).ToListAsync();
            var vaiTros = await _context.VaiTro.Select(v => new { v.MaVaiTro, v.TenVaiTro }).ToListAsync();

            return Ok(new { success = true, phongBans, vaiTros });
        }

        public class UpdateRoleDeptRequest
        {
            public int? MaPhongBan { get; set; }
            public List<int> MaVaiTros { get; set; }
        }

        // 3. CẬP NHẬT PHÒNG BAN VÀ VAI TRÒ
        [HttpPut("cap-nhat/{id}")]
        public async Task<IActionResult> UpdateNhanVien(int id, [FromBody] UpdateRoleDeptRequest request)
        {
            var nv = await _context.NhanVien.Include(n => n.NhanVienVaiTro).FirstOrDefaultAsync(n => n.MaNhanVien == id);
            if (nv == null) return NotFound("Không tìm thấy nhân viên.");

            nv.MaPhongBan = request.MaPhongBan;

            _context.NhanVienVaiTro.RemoveRange(nv.NhanVienVaiTro);
            if (request.MaVaiTros != null && request.MaVaiTros.Any())
            {
                foreach (var roleId in request.MaVaiTros)
                {
                    _context.NhanVienVaiTro.Add(new NhanVienVaiTro { MaNhanVien = id, MaVaiTro = roleId });
                }
            }

            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Cập nhật chức vụ thành công!" });
        }

        // 4. API KHÓA / MỞ KHÓA TÀI KHOẢN
        [HttpPut("toggle-status/{id}")]
        public async Task<IActionResult> ToggleStatusNhanVien(int id)
        {
            var nv = await _context.NhanVien.FindAsync(id);
            if (nv == null) return NotFound("Không tìm thấy nhân viên.");

            if (nv.TrangThai == 0)
                nv.TrangThai = 1; // Mở khóa
            else
                nv.TrangThai = 0; // Khóa

            _context.NhanVien.Update(nv);
            await _context.SaveChangesAsync();

            string msg = nv.TrangThai == 0
                ? "Đã khóa tài khoản thành công! Nhân viên này sẽ không thể đăng nhập."
                : "Đã mở khóa tài khoản thành công!";

            return Ok(new { success = true, message = msg });
        }

        // =========================================================
        // 5. API THÊM MỚI NHÂN VIÊN (CÓ MÃ HÓA MẬT KHẨU SHA256)
        // =========================================================
        public class CreateNhanVienRequest
        {
            public string HoTen { get; set; }
            public string Email { get; set; }
            public string MatKhau { get; set; }
        }

        [HttpPost("them")]
        public async Task<IActionResult> AddNhanVien([FromBody] CreateNhanVienRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.HoTen) || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.MatKhau))
            {
                return BadRequest(new { success = false, message = "Vui lòng nhập đầy đủ Họ tên, Email và Mật khẩu." });
            }

            // Kiểm tra trùng lặp Email
            bool isEmailExist = await _context.NhanVien.AnyAsync(nv => nv.Email == request.Email.Trim());
            if (isEmailExist)
            {
                return BadRequest(new { success = false, message = "Email này đã được sử dụng bởi nhân viên khác!" });
            }

            var nvMoi = new NhanVien
            {
                HoTen = request.HoTen.Trim(),
                Email = request.Email.Trim(),
                MatKhauHash = ComputeSha256Hash(request.MatKhau), // Mã hóa SHA256
                TrangThai = 1, // Mặc định là đang hoạt động
                NgayTao = DateTime.UtcNow // Tránh lỗi Timezone ở PostgreSQL
            };

            _context.NhanVien.Add(nvMoi);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Tạo tài khoản nhân viên thành công!" });
        }

        // Hàm hỗ trợ mã hóa SHA256
        private static string ComputeSha256Hash(string rawData)
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
    }
}