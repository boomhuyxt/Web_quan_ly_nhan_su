using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Web_quan_ly_nhan_su.Context;

namespace Web_quan_ly_nhan_su.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ChatController(AppDbContext context)
        {
            _context = context;
        }

        // 1. API LẤY LỊCH SỬ CHAT 1-1
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

        // 2. API TÌM KIẾM NHÂN VIÊN
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

        // 3. (MỚI) API LẤY DANH SÁCH TRÒ CHUYỆN GẦN ĐÂY KÈM DẤU CHẤM ĐỎ
        [HttpGet("conversations/{currentUserId}")]
        public async Task<IActionResult> GetRecentConversations(int currentUserId)
        {
            // Tìm tất cả những người đã từng chat với mình
            var partnerIds = await _context.TinNhan
                .Where(t => t.NguoiGuiId == currentUserId || t.NguoiNhanId == currentUserId)
                .Select(t => t.NguoiGuiId == currentUserId ? t.NguoiNhanId : t.NguoiGuiId)
                .Distinct()
                .ToListAsync();

            var conversations = new List<object>();

            foreach (var pId in partnerIds)
            {
                // Lấy tin nhắn cuối cùng để hiển thị preview
                var lastMsg = await _context.TinNhan
                    .Where(t => (t.NguoiGuiId == currentUserId && t.NguoiNhanId == pId) ||
                                (t.NguoiGuiId == pId && t.NguoiNhanId == currentUserId))
                    .OrderByDescending(t => t.ThoiGianGui)
                    .FirstOrDefaultAsync();

                // Đếm số tin nhắn chưa đọc (NguoiGui là pId, NguoiNhan là mình, và DaDoc == false)
                var unreadCount = await _context.TinNhan
                    .CountAsync(t => t.NguoiGuiId == pId && t.NguoiNhanId == currentUserId && !t.DaDoc);

                var friend = await _context.NhanVien.FindAsync(pId);

                conversations.Add(new
                {
                    FriendId = pId,
                    FriendName = friend?.HoTen,
                    FriendAvatar = friend?.AnhDaiDien,
                    LastMessageContent = lastMsg?.NoiDung,
                    LastMessageTime = lastMsg?.ThoiGianGui.AddHours(7).ToString("hh:mm tt"),
                    HasUnread = unreadCount > 0, // Cờ quyết định hiện chấm đỏ
                    RawTime = lastMsg?.ThoiGianGui
                });
            }

            // Sắp xếp cuộc hội thoại nào có tin nhắn mới nhất lên đầu
            var result = conversations.OrderByDescending(c => (System.DateTime?)c.GetType().GetProperty("RawTime").GetValue(c, null)).ToList();

            return Ok(result);
        }

        // 4. (MỚI) API ĐÁNH DẤU LÀ ĐÃ ĐỌC
        [HttpPost("mark-read/{currentUserId}/{friendId}")]
        public async Task<IActionResult> MarkAsRead(int currentUserId, int friendId)
        {
            // Tìm các tin nhắn người kia gửi cho mình mà mình chưa đọc
            var unreadMessages = await _context.TinNhan
                .Where(t => t.NguoiGuiId == friendId && t.NguoiNhanId == currentUserId && !t.DaDoc)
                .ToListAsync();

            if (unreadMessages.Any())
            {
                foreach (var msg in unreadMessages)
                {
                    msg.DaDoc = true; // Chuyển thành đã đọc
                }
                await _context.SaveChangesAsync();
            }
            return Ok(new { success = true });
        }
    }
}