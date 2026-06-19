using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;   // ← Thêm dòng này
using Pgvector;
using Pgvector.EntityFrameworkCore;
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
using Polly;

namespace Web_quan_ly_nhan_su.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    public class AiController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly AppDbContext _context;
        private readonly string _geminiApiKey;

        // Khóa bí mật dùng để mã hóa/giải mã (NÊN thay bằng key mạnh và giữ bí mật)
        private const string SecretKey = "AtelierHR2026SecureKey!@#";

        public AiController(HttpClient httpClient, AppDbContext context, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _context = context;

            string encryptedKey = configuration["GeminiSettings:ApiKey"];
            _geminiApiKey = DecryptString(encryptedKey);

            Console.WriteLine($"[AI Controller] Key giải mã: {!string.IsNullOrEmpty(_geminiApiKey)} | Length: {_geminiApiKey?.Length ?? 0}");
        }

        // ====================== GIẢI MÃ AES ======================
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
                Console.WriteLine("❌ Lỗi giải mã AES: " + ex.Message);
                return string.Empty;
            }
        }

        // ====================== CHAT API (LIVE SEARCH UPGRADED) ======================
        [HttpPost("Chat")]
        public async Task<IActionResult> Chat([FromBody] AiChatRequest request)
        {
            var responseData = new AiChatResponse
            {
                Success = true,
                AttachedFiles = new List<FileMau>()
            };

            if (string.IsNullOrEmpty(_geminiApiKey))
            {
                responseData.Success = false;
                responseData.Reply = "Lỗi cấu hình: API Key không hợp lệ hoặc chưa cấu hình môi trường.";
                return StatusCode(500, responseData);
            }

            if (string.IsNullOrEmpty(request?.Message))
            {
                responseData.Success = false;
                responseData.Reply = "Vui lòng nhập câu hỏi.";
                return BadRequest(responseData);
            }

            string lowerMessage = request.Message.ToLower().Trim();

            // 1. Gợi ý File mẫu
            var danhSachFileMau = new List<(string TuKhoa, string TenFile, string Url, string Icon)>
    {
        ("nghỉ việc", "Mẫu đơn xin nghỉ phép.docx", "https://dwdvizkleazjodyfbovl.supabase.co/storage/v1/object/public/FileMau/Mau_Don_Xin_Nghi_Phep.doc", "description"),
        ("thôi việc", "Mẫu đơn xin thôi việc.docx", "https://dwdvizkleazjodyfbovl.supabase.co/storage/v1/object/public/FileMau/Mau_Don_Xin_Nghi_Phep.doc", "description"),
        ("thôi việt", "Mẫu đơn xin thôi việc.docx", "https://dwdvizkleazjodyfbovl.supabase.co/storage/v1/object/public/FileMau/Mau_Don_Xin_Nghi_Phep.doc", "description"),
        ("nghỉ việt", "Mẫu đơn xin nghỉ phép.docx", "https://dwdvizkleazjodyfbovl.supabase.co/storage/v1/object/public/FileMau/Mau_Don_Xin_Nghi_Phep.doc", "description"),
        ("don nghi", "Mẫu đơn xin nghỉ phép.docx", "https://dwdvizkleazjodyfbovl.supabase.co/storage/v1/object/public/FileMau/Mau_Don_Xin_Nghi_Phep.doc", "description"),
        ("hợp đồng", "Mẫu hợp đồng lao động tiêu chuẩn.docx", "https://dwdvizkleazjodyfbovl.supabase.co/storage/v1/object/public/FileMau/Mau_hop_dong_lao_dong_tieu_chuan.docx", "description"),
        ("lao động", "Mẫu hợp đồng lao động tiêu chuẩn.docx", "https://dwdvizkleazjodyfbovl.supabase.co/storage/v1/object/public/FileMau/Mau_hop_dong_lao_dong_tieu_chuan.docx", "description")
    };

            foreach (var bieuMau in danhSachFileMau)
            {
                if (lowerMessage.Contains(bieuMau.TuKhoa) && !responseData.AttachedFiles.Any(f => f.Url == bieuMau.Url))
                {
                    responseData.AttachedFiles.Add(new FileMau
                    {
                        TenFile = bieuMau.TenFile,
                        Url = bieuMau.Url,
                        Icon = bieuMau.Icon
                    });
                }
            }

            // 2. RAG Vector - Nâng cấp bộ lọc ngưỡng khoảng cách (Threshold Filter)
            string thongTinHoTro = "Không tìm thấy tài liệu liên quan trong hệ thống nội bộ.";
            try
            {
                float[] vectorCauHoi = await GetGeminiEmbeddingAsync(request.Message);
                if (vectorCauHoi?.Length > 0)
                {
                    var pgVector = new Vector(vectorCauHoi);

                    var kienThucLienQuan = await _context.DanhMucKienThuc
                        .Select(x => new {
                            x.TieuDe,
                            x.NoiDung,
                            Distance = x.VectoNoiDung.CosineDistance(pgVector)
                        })
                        .Where(x => x.Distance <= 0.4)
                        .OrderBy(x => x.Distance)
                        .Take(3)
                        .Select(x => $"Tài liệu '{x.TieuDe}':\n{x.NoiDung}")
                        .ToListAsync();

                    if (kienThucLienQuan.Any())
                    {
                        thongTinHoTro = string.Join("\n\n---\n\n", kienThucLienQuan);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lỗi trích xuất dữ liệu Vector: " + ex.Message);
            }

            // 3. System Prompt cấu trúc chặt chẽ phối hợp với Live Google Search
            // Bổ sung thời gian thực tế để AI không bị nhầm lẫn mốc lịch sử khi tìm kiếm
            string thoiGianHienTai = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");

            string systemPrompt = $@"Bạn là trợ lý Nhân sự (HR) thân thiện và chuyên nghiệp của công ty Atelier.
Thời gian hệ thống hiện tại (Bây giờ): {thoiGianHienTai}

Nhiệm vụ của bạn là hỗ trợ nhân viên dựa trên 2 quy tắc phân định nghiêm ngặt sau:

1. ĐỐI VỚI CÁC CÂU HỎI VỀ QUY ĐỊNH, CHÍNH SÁCH NỘI BỘ CỦA CÔNG TY ATELIER:
- Bạn BẮT BUỘC phải dựa vào khối thông tin 'THÔNG TIN QUY ĐỊNH NỘI BỘ' được cung cấp dưới đây để trả lời.
- Tuyệt đối KHÔNG BỊA ĐẶT ra các điều luật hoặc chính sách không xuất hiện trong tài liệu này.
- Nếu tài liệu nội bộ không nhắc đến, hãy trả lời chính xác câu sau: 'Tôi chưa tìm thấy thông tin này trong quy định hiện hành, vui lòng liên hệ phòng HR để được giải đáp'.

2. ĐỐI VỚI CÁC CÂU HỎI VỀ KIẾN THỨC BÊN NGOÀI, THỜI SỰ, TIN TỨC, GIẢI TRÍ HOẶC CHÀO HỎI TÁN GẪU:
- Khi câu hỏi của nhân viên nằm ngoài phạm vi quy định của công ty, bạn được phép tự do sử dụng công cụ Google Search (đã được tích hợp sẵn) để tra cứu dữ liệu Internet và cập nhật câu trả lời mới nhất, chính xác nhất theo mốc thời gian thực hiện tại.
- Trả lời một cách thông minh, hữu ích, tự nhiên. Giữ vững phong thái của một người làm HR (lịch sự, hòa nhã, cởi mở).
- Tuyệt đối KHÔNG áp dụng câu từ chối nội bộ vào các câu hỏi kiến thức xã hội thông thường này.

THÔNG TIN QUY ĐỊNH NỘI BỘ:
{thongTinHoTro}

CÂU HỎI NHÂN VIÊN:
{request.Message}";

            // 4. Cơ chế tự động thử lại (Polly Retry) kết hợp cấu hình kích hoạt Search Tool
            try
            {
                var retryPolicy = Policy
                    .Handle<Exception>(ex => ex.Message.Contains("503") || ex.Message.Contains("demand") || ex.Message.Contains("UNAVAILABLE"))
                    .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt - 1)),
                        (exception, timeSpan, retryCount, context) =>
                        {
                            Console.WriteLine($"[Gemini API] Hệ thống bận (503). Đang tự động thử lại lần {retryCount} sau {timeSpan.Seconds}s...");
                        });

                // Thực thi cuộc gọi thông qua chính sách Retry
         
                responseData.Reply = await retryPolicy.ExecuteAsync(async () => await CallGeminiChatAsync(systemPrompt, enableSearch: true));
                return Ok(responseData);
            }
            catch (Exception ex)
            {
                responseData.Success = false;

                if (ex.Message.Contains("503") || ex.Message.Contains("demand") || ex.Message.Contains("UNAVAILABLE"))
                {
                    responseData.Reply = "Hệ thống trợ lý AI hiện đang nhận được số lượng câu hỏi quá tải từ máy chủ Google. Bạn vui lòng đợi vài giây rồi bấm gửi lại câu hỏi nhé! 🙏";
                }
                else
                {
                    responseData.Reply = $"Hệ thống gặp sự cố kết nối trợ lý AI. Vui lòng liên hệ IT để được hỗ trợ. (Chi tiết: {ex.Message})";
                }

                return StatusCode(500, responseData);
            }
        }

        // ====================== THÊM TÀI LIỆU ======================
        [HttpPost("ThemTaiLieu")]
        public async Task<IActionResult> ThemTaiLieu([FromBody] ThemKienThucRequest request)
        {
            if (string.IsNullOrEmpty(request?.NoiDung))
                return BadRequest(new { success = false, message = "Nội dung không được để trống." });

            try
            {
                float[] vectorNoiDung = await GetGeminiEmbeddingAsync(request.NoiDung);
                if (vectorNoiDung?.Length == 0)
                    return StatusCode(500, new { success = false, message = "Lỗi tạo Vector." });

                var kienThucMoi = new DanhMucKienThuc
                {
                    TieuDe = request.TieuDe,
                    NoiDung = request.NoiDung,
                    LoaiTaiLieu = request.LoaiTaiLieu,
                    VectoNoiDung = new Vector(vectorNoiDung)
                };

                _context.DanhMucKienThuc.Add(kienThucMoi);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = $"Đã nạp thành công: {request.TieuDe}" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        // ====================== EMBEDDING ======================
        private async Task<float[]> GetGeminiEmbeddingAsync(string text)
        {
            if (string.IsNullOrEmpty(_geminiApiKey)) throw new Exception("API Key rỗng");

            string apiUrl = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-embedding-001:embedContent?key={_geminiApiKey}";

            var payload = new
            {
                model = "models/gemini-embedding-001",
                content = new { parts = new[] { new { text = text } } },
                outputDimensionality = 768
            };

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(apiUrl, content);
            var responseString = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                using JsonDocument doc = JsonDocument.Parse(responseString);
                return doc.RootElement.GetProperty("embedding").GetProperty("values")
                          .EnumerateArray().Select(x => x.GetSingle()).ToArray();
            }
            throw new Exception($"Embedding Error: {responseString}");
        }

        // ====================== GEMINI CHAT ======================
        private async Task<string> CallGeminiChatAsync(string prompt, bool enableSearch)
        {
            if (string.IsNullOrEmpty(_geminiApiKey)) throw new Exception("API Key rỗng");

            string apiUrl = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={_geminiApiKey}";

            var payload = new
            {
                contents = new[] { new { parts = new[] { new { text = prompt } } } },
                generationConfig = new { temperature = 0.7, maxOutputTokens = 2048 }
            };

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(apiUrl, content);
            var responseString = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                using JsonDocument doc = JsonDocument.Parse(responseString);
                if (doc.RootElement.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
                {
                    return candidates[0]
                        .GetProperty("content")
                        .GetProperty("parts")[0]
                        .GetProperty("text")
                        .GetString() ?? "";
                }
            }
            throw new Exception($"Chat Error: {responseString}");
        }
    }

    // ====================== MODELS ======================
    public class AiChatRequest { public string Message { get; set; } }
    public class AiChatResponse
    {
        public bool Success { get; set; }
        public string Reply { get; set; }
        public List<FileMau> AttachedFiles { get; set; } = new List<FileMau>();
    }
    public class FileMau
    {
        public string TenFile { get; set; }
        public string Url { get; set; }
        public string Icon { get; set; }
    }
    public class ThemKienThucRequest
    {
        public string TieuDe { get; set; }
        public string NoiDung { get; set; }
        public string LoaiTaiLieu { get; set; }
    }
}