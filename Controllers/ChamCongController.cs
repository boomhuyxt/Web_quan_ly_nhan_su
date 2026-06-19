using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Web_quan_ly_nhan_su.Context;
using Web_quan_ly_nhan_su.Models;

namespace Web_quan_ly_nhan_su.Controllers
{
    public class ChamCongController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration; // ĐỂ SỬA: Khai báo trường cấu hình toàn cục
        private readonly HttpClient _httpClient;       // TỐI ƯU: Tái sử dụng HttpClient tránh lỗi nghẽn Socket

        // Khóa bí mật giải mã AES (Bắt buộc trùng khớp với SecretKey bên AiController)
        private const string SecretKey = "AtelierHR2026SecureKey!@#";

        public ChamCongController(AppDbContext context, IConfiguration configuration, HttpClient httpClient)
        {
            _context = context;
            _configuration = configuration;
            _httpClient = httpClient;
        }

        public class FaceRequest
        {
            public string ImageBase64 { get; set; }
        }

        // =========================================================
        // HÀM HỖ TRỢ: GIẢI MÃ AES API KEY
        // =========================================================
        private string DecryptString(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText)) return string.Empty;

            try
            {
                cipherText = cipherText.Trim().Replace(" ", "").Replace("_", "/");
                if (cipherText.Length % 4 != 0)
                    cipherText += new string('=', 4 - (cipherText.Length % 4));

                byte[] cipherBytes = Convert.FromBase64String(cipherText);

                using Aes aes = Aes.Create();
                aes.Key = Encoding.UTF8.GetBytes(SecretKey.PadRight(32).Substring(0, 32));
                aes.IV = aes.Key.Take(16).ToArray();
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using var ms = new MemoryStream();
                using var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write);
                cs.Write(cipherBytes, 0, cipherBytes.Length);
                cs.FlushFinalBlock();

                return Encoding.UTF8.GetString(ms.ToArray());
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Lỗi giải mã AES tại ChamCong: " + ex.Message);
                return string.Empty;
            }
        }

        // =========================================================
        // HÀM HỖ TRỢ: LẤY ID NHÂN VIÊN ĐANG ĐĂNG NHẬP THỰC TẾ
        // =========================================================
        private int GetCurrentUserId()
        {
            int userId = HttpContext.Session.GetInt32("MaNhanVien") ?? 0;

            if (userId == 0)
            {
                var claim = User.Claims.FirstOrDefault(c => c.Type == "MaNhanVien")?.Value
                         ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                int.TryParse(claim, out userId);
            }

            return userId;
        }

        // =========================================================
        // 1. API ĐĂNG KÝ KHUÔN MẶT
        // =========================================================
        [HttpPost]
        public async Task<IActionResult> DangKyKhuonMat([FromBody] FaceRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request?.ImageBase64))
                    return Json(new { success = false, message = "Không nhận được dữ liệu ảnh." });

                int maNhanVienCurrent = GetCurrentUserId();
                if (maNhanVienCurrent <= 0)
                    return Json(new { success = false, message = "Phiên đăng nhập đã hết hạn, vui lòng đăng nhập lại." });

                var nhanVien = await _context.NhanVien.FindAsync(maNhanVienCurrent);
                if (nhanVien == null)
                    return Json(new { success = false, message = "Không tìm thấy thông tin nhân viên." });

                nhanVien.FaceVector = request.ImageBase64;
                _context.NhanVien.Update(nhanVien);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Đăng ký FaceID thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi server: " + ex.Message });
            }
        }

        // =========================================================
        // 2. API NHẬN DIỆN VÀ CHẤM CÔNG KHUÔN MẶT (INTEGRATED GEMINI VISION)
        // =========================================================
        [HttpPost]
        public async Task<IActionResult> NhanDienVaChamCong([FromBody] FaceRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request?.ImageBase64))
                    return Json(new { success = false, message = "Không nhận được dữ liệu hình ảnh từ camera." });

                int maNhanVienCurrent = GetCurrentUserId();
                if (maNhanVienCurrent <= 0)
                    return Json(new { success = false, message = "Phiên đăng nhập đã hết hạn, vui lòng đăng nhập lại." });

                var nhanVien = await _context.NhanVien.FindAsync(maNhanVienCurrent);

                if (nhanVien == null || string.IsNullOrEmpty(nhanVien.FaceVector))
                    return Json(new { success = false, message = "Bạn chưa đăng ký khuôn mặt trên hệ thống!" });

                // XỬ LÝ SO SÁNH KHUÔN MẶT BẰNG GEMINI VISION API
                bool isMatch = false;
                try
                {
                    string apiKeyEncrypted = _configuration["GeminiSettings:ApiKey"];
                    string geminiApiKey = DecryptString(apiKeyEncrypted);

                    if (string.IsNullOrEmpty(geminiApiKey))
                        throw new Exception("Không thể giải mã API Key của hệ thống AI.");

                    string apiUrl = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={geminiApiKey}";

                    var payload = new
                    {
                        contents = new[]
                        {
                            new
                            {
                                parts = new object[]
                                {
                                    new { text = "Bạn là hệ thống kiểm soát ra vào bằng FaceID cấp cao. Hãy so sánh hai bức ảnh khuôn mặt dưới đây. Ảnh 1 là ảnh đăng ký gốc trong cơ sở dữ liệu. Ảnh 2 là ảnh quét thực tế từ camera chấm công. Kiểm tra kỹ các đường nét khuôn mặt, mắt, mũi, miệng, cấu trúc xương. Trả về chính xác từ 'TRUE' nếu hai ảnh là cùng một người, hoặc 'FALSE' nếu là hai người khác nhau hoặc phát hiện giả mạo. Không giải thích gì thêm." },
                                    new { inline_data = new { mime_type = "image/jpeg", data = nhanVien.FaceVector } },
                                    new { inline_data = new { mime_type = "image/jpeg", data = request.ImageBase64 } }
                                }
                            }
                        },
                        generationConfig = new { temperature = 0.1 }
                    };

                    var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                    var response = await _httpClient.PostAsync(apiUrl, content);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseString = await response.Content.ReadAsStringAsync();
                        using JsonDocument doc = JsonDocument.Parse(responseString);
                        string aiResult = doc.RootElement.GetProperty("candidates")[0]
                                            .GetProperty("content")
                                            .GetProperty("parts")[0]
                                            .GetProperty("text")
                                            .GetString()?.Trim().ToUpper() ?? "";

                        if (aiResult.Contains("TRUE"))
                        {
                            isMatch = true;
                        }
                    }
                    else
                    {
                        string errorResponse = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"[Gemini Vision Error] {errorResponse}");
                    }
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = "Lỗi hệ thống nhận diện FaceID: " + ex.Message });
                }

                if (!isMatch)
                    return Json(new { success = false, message = "Xác thực khuôn mặt thất bại! Khuôn mặt không khớp với FaceID đã đăng ký." });

                // TIẾN HÀNH GHI NHẬN CHẤM CÔNG NẾU KHUÔN MẶT KHỚP
                DateTime vietnamTime = DateTime.UtcNow.AddHours(7);
                DateTime homNay = DateTime.SpecifyKind(vietnamTime.Date, DateTimeKind.Utc);
                TimeSpan gioHienTai = vietnamTime.TimeOfDay;

                var chamCong = await _context.ChamCong
                    .FirstOrDefaultAsync(c => c.MaNhanVien == maNhanVienCurrent && c.NgayLamViec == homNay);

                if (chamCong == null)
                {
                    _context.ChamCong.Add(new ChamCong
                    {
                        MaNhanVien = maNhanVienCurrent,
                        NgayLamViec = homNay,
                        GioVao = gioHienTai
                    });
                }
                else
                {
                    chamCong.GioRa = gioHienTai;
                    _context.ChamCong.Update(chamCong);
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Chấm công thành công!", time = gioHienTai.ToString(@"hh\:mm") });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi máy chủ: " + ex.Message });
            }
        }

        // =========================================================
        // 3. API LẤY LỊCH SỬ CHẤM CÔNG 
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> GetLichSuChamCong()
        {
            try
            {
                int maNhanVienCurrent = GetCurrentUserId();
                if (maNhanVienCurrent <= 0)
                    return Json(new { success = true, data = new string[] { } });

                var lichSu = await _context.ChamCong
                    .Where(c => c.MaNhanVien == maNhanVienCurrent)
                    .OrderByDescending(c => c.NgayLamViec)
                    .Take(7)
                    .Select(c => new {
                        Ngay = c.NgayLamViec.ToString("dd/MM/yyyy"),
                        GioVao = c.GioVao.HasValue ? c.GioVao.Value.ToString(@"hh\:mm") : "--:--",
                        GioRa = c.GioRa.HasValue ? c.GioRa.Value.ToString(@"hh\:mm") : "--:--"
                    })
                    .ToListAsync();

                return Json(new { success = true, data = lichSu });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // =========================================================
        // 4. TRANG QUẢN LÝ XEM & SỬA CHẤM CÔNG (DÀNH CHO ADMIN/HR)
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> QuanLyChamCong(int? maNhanVien, int? thang, int? nam)
        {
            // Mặc định lấy tháng năm hiện tại nếu lọc trống
            int selectedThang = thang ?? DateTime.Now.Month;
            int selectedNam = nam ?? DateTime.Now.Year;

            // Gửi ngược lại sang View để giữ trạng thái trên bộ lọc select-option
            ViewBag.SelectedNhanVien = maNhanVien;
            ViewBag.Thang = selectedThang;
            ViewBag.Nam = selectedNam;

            // Lấy danh sách toàn bộ nhân viên để admin chọn lọc theo tên
            var dsNhanVien = await _context.NhanVien.OrderBy(n => n.HoTen).ToListAsync();

            // Query lọc bảng Chấm Công theo Tháng / Năm
            var query = _context.ChamCong.Include(c => c.NhanVien)
                .Where(c => c.NgayLamViec.Month == selectedThang && c.NgayLamViec.Year == selectedNam);

            // Nếu admin chọn đích danh một nhân viên
            if (maNhanVien.HasValue && maNhanVien > 0)
            {
                query = query.Where(c => c.MaNhanVien == maNhanVien.Value);
            }

            var dsChamCong = await query.OrderByDescending(c => c.NgayLamViec).ToListAsync();
            ViewBag.DsChamCong = dsChamCong;

            return View(dsNhanVien); // Trả Model là danh sách nhân viên về cho thanh Dropdown
        }

        // Model nhận dữ liệu sửa đổi từ AJAX
        public class CapNhatChamCongRequest
        {
            public int MaChamCong { get; set; }
            public int MaNhanVien { get; set; }
            public string NgayLamViec { get; set; }
            public string? GioVao { get; set; }
            public string? GioRa { get; set; }
            public bool IsChamCong { get; set; } // true: Đã đi làm, false: Xóa / Hủy chấm ngày đó
        }

        // =========================================================
        // 5. API CẬP NHẬT TRẠNG THÁI / GIỜ CHẤM CÔNG TỪ ADMIN
        // =========================================================
        [HttpPost]
        public async Task<IActionResult> CapNhatChamCongAdmin([FromBody] CapNhatChamCongRequest request)
        {
            try
            {
                if (request == null || request.MaNhanVien <= 0)
                    return Json(new { success = false, message = "Dữ liệu không hợp lệ." });

                DateTime ngayXuly = DateTime.Parse(request.NgayLamViec).Date;

                // Tìm bản ghi chấm công đã tồn tại chưa
                var chamCong = await _context.ChamCong
                    .FirstOrDefaultAsync(c => (request.MaChamCong > 0 && c.MaChamCong == request.MaChamCong)
                                           || (c.MaNhanVien == request.MaNhanVien && c.NgayLamViec == ngayXuly));

                if (!request.IsChamCong)
                {
                    // Trình trạng: Admin sửa thành "Chưa chấm công" (Hủy/Xóa ngày chấm công này)
                    if (chamCong != null)
                    {
                        _context.ChamCong.Remove(chamCong);
                        await _context.SaveChangesAsync();
                    }
                    return Json(new { success = true, message = "Đã cập nhật trạng thái thành: Chưa chấm công" });
                }
                else
                {
                    // Tình trạng: Admin muốn ghi nhận "Đã chấm công" hoặc sửa giờ làm cụ thể
                    TimeSpan? gioVaoParsed = !string.IsNullOrEmpty(request.GioVao) ? TimeSpan.Parse(request.GioVao) : new TimeSpan(8, 0, 0);
                    TimeSpan? gioRaParsed = !string.IsNullOrEmpty(request.GioRa) ? TimeSpan.Parse(request.GioRa) : new TimeSpan(17, 0, 0);

                    if (chamCong == null)
                    {
                        // Nếu ngày đó chưa có bản ghi (Chưa chấm công), tiến hành thêm mới trực tiếp
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
                        // Nếu đã có bản ghi trước đó, cập nhật lại khung giờ mới do Admin chỉ định
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
    }
}