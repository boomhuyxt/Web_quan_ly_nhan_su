using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Web_quan_ly_nhan_su.Context;
using Web_quan_ly_nhan_su.Models;
using Microsoft.AspNetCore.Http;

namespace Web_quan_ly_nhan_su.Controllers
{
    [Route("api/congtac")]
    [ApiController]
    public class CongTacController : ControllerBase
    {
        private readonly AppDbContext _context;

        // Cấu hình thông tin máy chủ lưu trữ Supabase
        private readonly string _supabaseUrl = "https://dwdvizkleazjodyfbovl.supabase.co";
        private readonly string _supabaseKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImR3ZHZpemtsZWF6am9keWZib3ZsIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NzY5NTMxNzcsImV4cCI6MjA5MjUyOTE3N30.Kf-Rp5oup1xGm-l8yjZfzY_3kGsOMotQCrqKJx6l88w";

        public CongTacController(AppDbContext context)
        {
            _context = context;
        }

        // ====================================================================
        // 1. API: THÊM LỊCH CÔNG TÁC (ĐÃ SỬA LỖI MAPPING ĐẨY FILE VÀO SUPABASE)
        // ====================================================================
        [HttpPost("them-co-file")]
        public async Task<IActionResult> ThemCongTacCoFile([FromForm] IFormFile? file, [FromForm] string jsonData)
        {
            try
            {
                if (string.IsNullOrEmpty(jsonData))
                    return BadRequest(new { success = false, message = "Dữ liệu văn bản trống." });

                // Giải mã chuỗi JSON nhận từ FormData thành đối tượng DTO chuẩn
                var request = Newtonsoft.Json.JsonConvert.DeserializeObject<ThemCongTacRequest>(jsonData);
                if (request == null)
                    return BadRequest(new { success = false, message = "Cấu trúc dữ liệu giải mã không hợp lệ." });

                // Validate kiểm tra nghiệp vụ logic
                if (request.MaNhanVien <= 0)
                    return BadRequest(new { success = false, message = "Vui lòng chọn nhân viên phối hợp công tác." });

                if (string.IsNullOrEmpty(request.NgayBatDau) || string.IsNullOrEmpty(request.NgayKetThuc))
                    return BadRequest(new { success = false, message = "Vui lòng chọn đầy đủ ngày bắt đầu và kết thúc." });

                // 👉 ĐÃ SỬA: Ép kiểu ngày tháng chủ động từ chuỗi string để loại bỏ hoàn toàn lỗi crash 500
                DateTime tuNgay = DateTime.Parse(request.NgayBatDau);
                DateTime denNgay = DateTime.Parse(request.NgayKetThuc);

                if (tuNgay > denNgay)
                    return BadRequest(new { success = false, message = "Ngày bắt đầu không được diễn ra sau ngày kết thúc." });

                string fileUrl = "";

                // Kết nối Server Supabase gửi file lên Bucket và lấy link công khai
                if (file != null && file.Length > 0)
                {
                    var supabase = new Supabase.Client(_supabaseUrl, _supabaseKey);
                    await supabase.InitializeAsync();

                    // Định dạng tên file duy nhất bằng chuỗi GUID tránh trùng lặp tệp tin
                    string fileName = $"{Guid.NewGuid()}_{file.FileName}";

                    using (var memoryStream = new MemoryStream())
                    {
                        await file.CopyToAsync(memoryStream);
                        byte[] fileBytes = memoryStream.ToArray(); // Giải quyết triệt để lỗi ép kiểu CS1503

                        // Đẩy file (gửi file) trực tiếp lên Bucket "Lich_CT" trên Supabase
                        await supabase.Storage.From("Lich_CT").Upload(fileBytes, fileName);

                        // Trích xuất Public URL công khai từ Server Supabase để lưu vào database
                        fileUrl = supabase.Storage.From("Lich_CT").GetPublicUrl(fileName);
                    }
                }

                // Khởi tạo thực thể cấu trúc DB và lưu xuống cơ sở dữ liệu
                var lichMoi = new LichCongTac
                {
                    MaNhanVien = request.MaNhanVien,
                    NgayBatDau = tuNgay,   // Gán biến DateTime đã parse an toàn
                    NgayKetThuc = denNgay, // Gán biến DateTime đã parse an toàn
                    DiaDiem = string.IsNullOrEmpty(request.DiaDiem) ? "Chưa xác định" : request.DiaDiem,
                    NoiDungCongViec = string.IsNullOrEmpty(request.NoiDungCongViec) ? "" : request.NoiDungCongViec,
                    FileDinhKemUrl = fileUrl, // Đường link lưu file lấy từ server Supabase
                    TrangThai = "Sắp tới",
                    NgayTao = DateTime.Now
                };

                _context.LichCongTacs.Add(lichMoi);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Đã xếp lịch công tác và đồng bộ file lên Supabase thành công!" });
            }
            catch (Exception ex)
            {
                var errorMsg = ex.Message;
                if (ex.InnerException != null)
                {
                    errorMsg += " | Chi tiết lỗi nội bộ: " + ex.InnerException.Message;
                }
                // Trả về chi tiết lỗi thực tế giúp bạn debug trực tiếp trên màn hình F12
                return StatusCode(500, new { success = false, message = "Lỗi hệ thống xử lý nội bộ: " + errorMsg });
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
                        TenNhanVien = x.NhanVien != null ? x.NhanVien.HoTen : "Không xác định",
                        AnhDaiDien = x.NhanVien != null ? (x.NhanVien.AnhDaiDien ?? "https://raw.githubusercontent.com/arcuri82/web_development_and_api_design/master/exercise-solutions/quiz-game/frontend/src/images/avatar_default.jpg") : "https://raw.githubusercontent.com/arcuri82/web_development_and_api_design/master/exercise-solutions/quiz-game/frontend/src/images/avatar_default.jpg"
                    })
                    .ToListAsync();

                return Ok(new { success = true, data = danhSach });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi tải danh sách: " + ex.Message });
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
                int currentUserId = HttpContext.Session.GetInt32("MaNhanVien") ?? 0;

                if (currentUserId == 0)
                {
                    var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "MaNhanVien")?.Value
                                   ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                    int.TryParse(userIdClaim, out currentUserId);
                }

                if (currentUserId <= 0)
                {
                    return Ok(new { success = true, data = new string[] { } });
                }

                var danhSach = await _context.LichCongTacs
                    .Include(x => x.NhanVien)
                    .Where(x => x.MaNhanVien == currentUserId)
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
                        TenNhanVien = x.NhanVien != null ? x.NhanVien.HoTen : "Không xác định",
                        AnhDaiDien = x.NhanVien != null ? (x.NhanVien.AnhDaiDien ?? "https://raw.githubusercontent.com/arcuri82/web_development_and_api_design/master/exercise-solutions/quiz-game/frontend/src/images/avatar_default.jpg") : "https://raw.githubusercontent.com/arcuri82/web_development_and_api_design/master/exercise-solutions/quiz-game/frontend/src/images/avatar_default.jpg"
                    })
                    .ToListAsync();

                return Ok(new { success = true, data = danhSach });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi tải dữ liệu cá nhân: " + ex.Message });
            }
        }
    }

    // 👉 ĐÃ SỬA: Chuyển kiểu dữ liệu từ DateTime sang string để tiếp nhận chuỗi HTML Date từ Client mà không bị crash
    public class ThemCongTacRequest
    {
        public int MaNhanVien { get; set; }
        public string NgayBatDau { get; set; }
        public string NgayKetThuc { get; set; }
        public string DiaDiem { get; set; }
        public string NoiDungCongViec { get; set; }
        public string? FileDinhKemUrl { get; set; }
    }
}