using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using Web_quan_ly_nhan_su.Context;

namespace Web_quan_ly_nhan_su.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TongQuanController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TongQuanController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("thong-ke")]
        public async Task<IActionResult> GetThongKeDashBoard()
        {
            try
            {
                // 1. Tổng nhân viên hiện tại 
                // (Có thể thêm .Where(nv => nv.TrangThai == 1) nếu bạn có cờ đánh dấu nhân viên đang làm việc)
                int tongNhanVien = await _context.NhanVien.CountAsync();

                // 2. Bao nhiêu phòng ban
                int tongPhongBan = await _context.PhongBan.CountAsync();

                // 3. Đơn chờ duyệt 
                // Giả định bảng NghiPhep của bạn có cột TrangThai, quy ước 0 là "Chờ duyệt"
                int donChoDuyet = await _context.NghiPhep.CountAsync(np => np.TrangThai == 0);

                // ==========================================
                // XỬ LÝ LOGIC CHẤM CÔNG TRONG NGÀY
                // ==========================================

                // Lấy ngày hôm nay theo múi giờ Việt Nam (UTC+7) chuẩn với PostgreSQL
                DateTime vietnamTime = DateTime.UtcNow.AddHours(7);
                DateTime homNay = DateTime.SpecifyKind(vietnamTime.Date, DateTimeKind.Utc);

                // Lấy danh sách những người ĐÃ chấm công (có Giờ Vào) trong hôm nay
                var danhSachChamCongHomNay = await _context.ChamCong
                    .Where(c => c.NgayLamViec == homNay && c.GioVao.HasValue)
                    .ToListAsync();

                int soNguoiDaChamCong = danhSachChamCongHomNay.Count;

                // 4. Số nhân viên CHƯA chấm công hôm nay
                int chuaChamCong = tongNhanVien - soNguoiDaChamCong;
                if (chuaChamCong < 0) chuaChamCong = 0; // Đề phòng lỗi logic dữ liệu

                // 5. Tính tỷ lệ đi làm đúng giờ (Giờ chuẩn là 08:00:00)
                TimeSpan gioVaoChuan = new TimeSpan(8, 0, 0);

                // Đếm số người có Giờ Vào <= 8h
                int diDungGio = danhSachChamCongHomNay.Count(c => c.GioVao.Value <= gioVaoChuan);
                int diTre = soNguoiDaChamCong - diDungGio;

                double tyLeDungGio = 0;
                if (soNguoiDaChamCong > 0)
                {
                    // Tính tỷ lệ % đúng giờ dựa trên những người ĐÃ ĐI LÀM hôm nay
                    tyLeDungGio = Math.Round((double)diDungGio / soNguoiDaChamCong * 100, 1);
                }

                // Trả về một khối JSON chứa tất cả thông tin
                return Ok(new
                {
                    success = true,
                    tongNhanVien = tongNhanVien,
                    tongPhongBan = tongPhongBan,
                    donChoDuyet = donChoDuyet,
                    chuaChamCong = chuaChamCong,
                    diDungGio = diDungGio,
                    diTre = diTre,
                    tyLeDungGio = tyLeDungGio
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi server: " + ex.Message });
            }
        }
    }
}