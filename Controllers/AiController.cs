using Microsoft.AspNetCore.Mvc;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Web_quan_ly_nhan_su.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AiController : ControllerBase
    {
        private readonly HttpClient _httpClient;

        // Thay chuỗi này bằng API Key thật của ông lấy từ Google AI Studio
        private readonly string _geminiApiKey = "AIzaSyASfoGRGDy9XGAHiLVW927WZ_VqsTj1re8";

        public AiController(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        [HttpPost("Chat")]
        public async Task<IActionResult> Chat([FromBody] ChatRequest request)
        {
            if (string.IsNullOrEmpty(request.Message))
            {
                return BadRequest(new { success = false, reply = "Vui lòng nhập câu hỏi." });
            }

            // Sử dụng model gemini-1.5-flash cho tốc độ phản hồi nhanh
            string apiUrl = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={_geminiApiKey}";

            // Định dạng payload JSON theo đúng chuẩn yêu cầu của Google API
            var payload = new
            {
                contents = new[]
                {
                    new { parts = new[] { new { text = request.Message } } }
                }
            };

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            try
            {
                // Gửi HTTP POST tới Google
                var response = await _httpClient.PostAsync(apiUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    var responseString = await response.Content.ReadAsStringAsync();

                    // Parse kết quả trả về để trích xuất đúng đoạn text câu trả lời
                    using JsonDocument doc = JsonDocument.Parse(responseString);
                    var replyText = doc.RootElement
                                       .GetProperty("candidates")[0]
                                       .GetProperty("content")
                                       .GetProperty("parts")[0]
                                       .GetProperty("text")
                                       .GetString();

                    return Ok(new { success = true, reply = replyText });
                }
                else
                {
                    // Đọc nội dung lỗi nếu API key sai hoặc quá giới hạn
                    var errorDetail = await response.Content.ReadAsStringAsync();
                    return StatusCode(500, new { success = false, reply = "Lỗi từ máy chủ AI.", details = errorDetail });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, reply = "Hệ thống gặp sự cố: " + ex.Message });
            }
        }
    }

    // Model để hứng dữ liệu gửi lên từ Javascript (fetch)
    public class ChatRequest
    {
        public string Message { get; set; }
    }
}