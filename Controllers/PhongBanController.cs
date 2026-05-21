using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using Web_quan_ly_nhan_su.Context;
using Web_quan_ly_nhan_su.Models; // Sử dụng namespace chứa model PhongBan

namespace Web_quan_ly_nhan_su.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PhongBanController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PhongBanController(AppDbContext context)
        {
            _context = context;
        }

        // Lấy danh sách tất cả phòng ban
        [HttpGet("danh-sach")]
        public async Task<IActionResult> GetAll()
        {
            var data = await _context.PhongBan
                .Select(p => new { p.MaPhongBan, p.TenPhongBan })
                .ToListAsync();
            return Ok(new { success = true, data });
        }

        // Tạo mới phòng ban
        [HttpPost("them")]
        public async Task<IActionResult> Create([FromBody] PhongBan phongBan)
        {
            if (string.IsNullOrEmpty(phongBan.TenPhongBan))
                return BadRequest(new { success = false, message = "Tên phòng ban không được để trống." });

            _context.PhongBan.Add(phongBan);
            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Thêm phòng ban thành công!" });
        }

        // Cập nhật tên phòng ban
        [HttpPut("cap-nhat/{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] PhongBan phongBan)
        {
            var pb = await _context.PhongBan.FindAsync(id);
            if (pb == null) return NotFound(new { success = false, message = "Không tìm thấy phòng ban." });

            pb.TenPhongBan = phongBan.TenPhongBan;
            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Cập nhật thành công!" });
        }

        // Xóa phòng ban
        [HttpDelete("xoa/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var pb = await _context.PhongBan.Include(p => p.NhanViens).FirstOrDefaultAsync(p => p.MaPhongBan == id);

            if (pb == null) return NotFound(new { success = false, message = "Không tìm thấy phòng ban." });

            // Kiểm tra xem phòng ban có nhân viên nào không trước khi xóa
            if (pb.NhanViens != null && pb.NhanViens.Any())
            {
                return BadRequest(new { success = false, message = "Không thể xóa vì phòng ban này đang có nhân viên." });
            }

            _context.PhongBan.Remove(pb);
            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Xóa phòng ban thành công!" });
        }
    }
}