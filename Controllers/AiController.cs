using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Web_quan_ly_nhan_su.Context;
using Web_quan_ly_nhan_su.Models;

namespace Web_quan_ly_nhan_su.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous] // Cho phép nạp dữ liệu từ Postman (Xóa đi nếu đã nạp xong)
    public class AiController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly AppDbContext _context;
        private readonly string _geminiApiKey; // Đã bảo mật: Không gán cứng ở đây

        public AiController(HttpClient httpClient, AppDbContext context, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _context = context;
            // Đọc Key từ file appsettings.json
            _geminiApiKey = configuration["GeminiSettings:ApiKey"];
        }

        [HttpPost("Chat")]
        public async Task<IActionResult> Chat([FromBody] AiChatRequest request)
        {
            var responseData = new AiChatResponse { Success = true };
            if (string.IsNullOrEmpty(request.Message))
            {
                responseData.Success = false;
                responseData.Reply = "Vui lòng nhập câu hỏi.";
                return BadRequest(responseData);
            }

            string lowerMessage = request.Message.ToLower();

            // 1. LOGIC GỬI FILE MẪU TRỰC TIẾP
            var danhSachFileMau = new List<(string TuKhoa, string TenFile, string Url, string Icon)>
            {
                ("nghỉ việc", "Mẫu đơn xin thôi việc.docx", "https://dwdvizkleazjodyfbovl.supabase.co/storage/v1/object/public/FileMau/Mau_Don_Xin_Nghi_Phep.doc", "description"),
                ("thôi việc", "Mẫu đơn xin thôi việc.docx", "https://dwdvizkleazjodyfbovl.supabase.co/storage/v1/object/public/FileMau/Mau_Don_Xin_Nghi_Phep.doc", "description"),
                ("nghỉ việt", "Mẫu đơn xin thôi việc.docx", "https://dwdvizkleazjodyfbovl.supabase.co/storage/v1/object/public/FileMau/Mau_Don_Xin_Nghi_Phep.doc", "description"),
                ("hợp đồng", "Mẫu hợp đồng lao động tiêu chuẩn.docx", "https://dwdvizkleazjodyfbovl.supabase.co/storage/v1/object/public/FileMau/Mau_hop_dong_lao_dong_tieu_chuan.docx", "description"),
                ("lao động", "Mẫu hợp đồng lao động tiêu chuẩn.docx", "https://dwdvizkleazjodyfbovl.supabase.co/storage/v1/object/public/FileMau/Mau_hop_dong_lao_dong_tieu_chuan.docx", "description")
            };

            var fileDaThem = new HashSet<string>();
            foreach (var bieuMau in danhSachFileMau)
            {
                if (lowerMessage.Contains(bieuMau.TuKhoa) && !fileDaThem.Contains(bieuMau.Url))
                {
                    responseData.AttachedFiles.Add(new FileMau
                    {
                        TenFile = bieuMau.TenFile,
                        Url = bieuMau.Url,
                        Icon = bieuMau.Icon
                    });
                    fileDaThem.Add(bieuMau.Url);
                }
            }

            // 2. RAG: TÌM KIẾM VECTOR
            string thongTinHoTro = "Không tìm thấy tài liệu liên quan trong hệ thống.";
            try
            {
                float[] vectorCauHoi = await GetGeminiEmbeddingAsync(request.Message);
                if (vectorCauHoi != null && vectorCauHoi.Length > 0)
                {
                    var pgVector = new Vector(vectorCauHoi);
                    var kienThucLienQuan = await _context.DanhMucKienThuc
                        .OrderBy(x => x.VectoNoiDung.CosineDistance(pgVector))
                        .Take(2)
                        .Select(x => $"Tài liệu '{x.TieuDe}':\n{x.NoiDung}")
                        .ToListAsync();

                    if (kienThucLienQuan.Any())
                        thongTinHoTro = string.Join("\n\n---\n\n", kienThucLienQuan);
                }
            }
            catch (Exception ex) { Console.WriteLine("Lỗi Vector: " + ex.Message); }

            // 3. GỌI GEMINI CHAT
            string systemPrompt = $@"Bạn là trợ lý Nhân sự (HR) thân thiện của công ty Atelier.
Dựa vào các thông tin quy định nội bộ công ty cung cấp dưới đây, hãy trả lời câu hỏi của nhân viên. 
Tuyệt đối KHÔNG BỊA ĐẶT luật. Nếu tài liệu dưới đây không nhắc đến, hãy nói 'Tôi chưa tìm thấy thông tin này trong quy định hiện hành, vui lòng liên hệ phòng HR để được giải đáp'.

THÔNG TIN QUY ĐỊNH:{thongTinHoTro}
CÂU HỎI NHÂN VIÊN: {request.Message}";

            try
            {
                responseData.Reply = await CallGeminiChatAsync(systemPrompt);
                return Ok(responseData);
            }
            catch (Exception ex)
            {
                responseData.Success = false;
                responseData.Reply = "Hệ thống gặp sự cố kết nối AI: " + ex.Message;
                return StatusCode(500, responseData);
            }
        }

        [HttpPost("ThemTaiLieu")]
        public async Task<IActionResult> ThemTaiLieu([FromBody] ThemKienThucRequest request)
        {
            if (string.IsNullOrEmpty(request.NoiDung))
                return BadRequest(new { success = false, message = "Nội dung không được để trống." });

            try
            {
                float[] vectorNoiDung = await GetGeminiEmbeddingAsync(request.NoiDung);
                if (vectorNoiDung == null || vectorNoiDung.Length == 0)
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
                var realError = ex.InnerException?.Message ?? ex.Message;
                return StatusCode(500, new { success = false, message = "Lỗi Database: " + realError });
            }
        }

        private async Task<float[]> GetGeminiEmbeddingAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return Array.Empty<float>();
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
                return doc.RootElement.GetProperty("embedding").GetProperty("values").EnumerateArray().Select(x => x.GetSingle()).ToArray();
            }
            throw new Exception($"Embedding Error: {responseString}");
        }

        private async Task<string> CallGeminiChatAsync(string prompt)
        {
            string apiUrl = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={_geminiApiKey}";
            var payload = new { contents = new[] { new { parts = new[] { new { text = prompt } } } } };
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(apiUrl, content);
            var responseString = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                using JsonDocument doc = JsonDocument.Parse(responseString);
                return doc.RootElement.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString();
            }
            throw new Exception($"Chat Error: {responseString}");
        }
    }

    // Models 
    public class AiChatRequest { public string Message { get; set; } }
    public class AiChatResponse { public bool Success { get; set; } public string Reply { get; set; } public List<FileMau> AttachedFiles { get; set; } = new List<FileMau>(); }
    public class FileMau { public string TenFile { get; set; } public string Url { get; set; } public string Icon { get; set; } }
    public class ThemKienThucRequest { public string TieuDe { get; set; } public string NoiDung { get; set; } public string LoaiTaiLieu { get; set; } }
}