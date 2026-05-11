using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Web_quan_ly_nhan_su.Context;
using Web_quan_ly_nhan_su.Models;
using Supabase;
using System.IO;
using System;

namespace Web_quan_ly_nhan_su.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly string _supabaseUrl = "https://dwdvizkleazjodyfbovl.supabase.co";
        private readonly string _supabaseKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImR3ZHZpemtsZWF6am9keWZib3ZsIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NzY5NTMxNzcsImV4cCI6MjA5MjUyOTE3N30.Kf-Rp5oup1xGm-l8yjZfzY_3kGsOMotQCrqKJx6l88w";

        public ChatController(AppDbContext context)
        {
            _context = context;
        }

        // 1. LẤY LỊCH SỬ CHAT 1-1
        [HttpGet("history/{nguoiGuiId}/{nguoiNhanId}")]
        public async Task<IActionResult> GetChatHistory(int nguoiGuiId, int nguoiNhanId)
        {
            var lichSu = await (from t in _context.TinNhan
                                join nv in _context.NhanVien on t.NguoiGuiId equals nv.MaNhanVien
                                where (t.NguoiGuiId == nguoiGuiId && t.NguoiNhanId == nguoiNhanId) ||
                                      (t.NguoiGuiId == nguoiNhanId && t.NguoiNhanId == nguoiGuiId)
                                orderby t.ThoiGianGui ascending
                                select new
                                {
                                    t.NguoiGuiId,
                                    nv.HoTen,
                                    nv.AnhDaiDien,
                                    t.NoiDung,
                                    ThoiGian = t.ThoiGianGui.AddHours(7).ToString("hh:mm tt")
                                }).ToListAsync();

            return Ok(lichSu);
        }

        // 2. TÌM KIẾM NHÂN VIÊN
        [HttpGet("search")]
        public async Task<IActionResult> SearchUsers([FromQuery] string q, [FromQuery] int currentUserId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(q)) return Ok(new List<object>());

                var users = await _context.NhanVien
                    .Where(nv => nv.MaNhanVien != currentUserId && nv.HoTen.ToLower().Contains(q.ToLower()))
                    .Select(nv => new { nv.MaNhanVien, nv.HoTen, nv.AnhDaiDien })
                    .Take(15)
                    .ToListAsync();

                return Ok(users);
            }
            catch (System.Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // 3. LẤY DANH SÁCH TRÒ CHUYỆN GẦN ĐÂY
        [HttpGet("conversations/{currentUserId}")]
        public async Task<IActionResult> GetRecentConversations(int currentUserId)
        {
            var partnerIds = await _context.TinNhan
                .Where(t => t.NguoiGuiId == currentUserId || t.NguoiNhanId == currentUserId)
                .Select(t => t.NguoiGuiId == currentUserId ? t.NguoiNhanId : t.NguoiGuiId)
                .Distinct()
                .ToListAsync();

            var conversations = new List<object>();

            foreach (var pId in partnerIds)
            {
                var lastMsg = await _context.TinNhan
                    .Where(t => (t.NguoiGuiId == currentUserId && t.NguoiNhanId == pId) ||
                                (t.NguoiGuiId == pId && t.NguoiNhanId == currentUserId))
                    .OrderByDescending(t => t.ThoiGianGui)
                    .FirstOrDefaultAsync();

                var unreadCount = await _context.TinNhan
                    .CountAsync(t => t.NguoiGuiId == pId && t.NguoiNhanId == currentUserId && !t.DaDoc);

                var friend = await _context.NhanVien.FindAsync(pId);

                string previewMsg = lastMsg?.NoiDung;
                if (!string.IsNullOrEmpty(previewMsg) && previewMsg.StartsWith("[FILE]"))
                {
                    previewMsg = "📎 Đã gửi một tập tin";
                }

                conversations.Add(new
                {
                    FriendId = pId,
                    FriendName = friend?.HoTen,
                    FriendAvatar = friend?.AnhDaiDien,
                    LastMessageContent = previewMsg,
                    LastMessageTime = lastMsg?.ThoiGianGui.AddHours(7).ToString("hh:mm tt"),
                    HasUnread = unreadCount > 0,
                    RawTime = lastMsg?.ThoiGianGui
                });
            }

            var result = conversations.OrderByDescending(c => (System.DateTime?)c.GetType().GetProperty("RawTime").GetValue(c, null)).ToList();
            return Ok(result);
        }

        // 4. ĐÁNH DẤU ĐÃ ĐỌC
        [HttpPost("mark-read/{currentUserId}/{friendId}")]
        public async Task<IActionResult> MarkAsRead(int currentUserId, int friendId)
        {
            var unreadMessages = await _context.TinNhan
                .Where(t => t.NguoiGuiId == friendId && t.NguoiNhanId == currentUserId && !t.DaDoc)
                .ToListAsync();

            if (unreadMessages.Any())
            {
                foreach (var msg in unreadMessages)
                {
                    msg.DaDoc = true;
                }
                await _context.SaveChangesAsync();
            }
            return Ok(new { success = true });
        }

        // 5. API UPLOAD FILE CHAT (MỚI)
        [HttpPost("upload")]
        [RequestSizeLimit(52428800)]
        [RequestFormLimits(MultipartBodyLengthLimit = 52428800)]
        public async Task<IActionResult> UploadChatFile(IFormFile file, [FromForm] int senderId)
        {
            if (file == null || file.Length == 0) return BadRequest("File không hợp lệ.");

            if (file.Length > 52428800) return BadRequest("Dung lượng file không được vượt quá 50MB.");

            try
            {
                var options = new SupabaseOptions { AutoConnectRealtime = true };
                var supabase = new Supabase.Client(_supabaseUrl, _supabaseKey, options);
                await supabase.InitializeAsync();

                using var ms = new MemoryStream();
                await file.CopyToAsync(ms);
                var fileBytes = ms.ToArray();

                // ĐÃ SỬA: Đổi tên file tải lên Supabase chỉ chứa chuỗi Guid và đuôi file (tránh lỗi tiếng Việt/khoảng trắng)
                string fileExtension = Path.GetExtension(file.FileName);
                string uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";

                await supabase.Storage.From("chat-files").Upload(fileBytes, uniqueFileName);

                string fileUrl = supabase.Storage.From("chat-files").GetPublicUrl(uniqueFileName);

                // Dữ liệu lưu vào Database vẫn sử dụng tên file.FileName gốc (có dấu tiếng Việt) bình thường
                var luuTru = new LuuTruFile
                {
                    TenFile = file.FileName,
                    LoaiFile = file.ContentType,
                    KichThuoc = file.Length,
                    DuongDan = fileUrl,
                    MaNhanVien = senderId,
                    NgayUpload = DateTime.UtcNow
                };

                _context.Add(luuTru);
                await _context.SaveChangesAsync();

                // Trả về dữ liệu cho Client
                return Ok(new { fileName = file.FileName, url = fileUrl });
            }
            catch (System.Exception ex)
            {
                string detailError = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return StatusCode(500, "Lỗi server: " + detailError);
            }
        }
    }
}