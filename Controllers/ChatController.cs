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

        // 3. LẤY DANH SÁCH TRÒ CHUYỆN GẦN ĐÂY (CÁ NHÂN + NHÓM)
        [HttpGet("conversations/{currentUserId}")]
        public async Task<IActionResult> GetRecentConversations(int currentUserId)
        {
            var conversations = new List<object>();

            // ==========================================
            // A. LẤY DANH SÁCH CHAT 1-1 (CÁ NHÂN)
            // ==========================================
            var partnerIds = await _context.TinNhan
                .Where(t => (t.NguoiGuiId == currentUserId || t.NguoiNhanId == currentUserId))
                .Select(t => t.NguoiGuiId == currentUserId ? t.NguoiNhanId : t.NguoiGuiId)
                .Distinct()
                .ToListAsync();

            foreach (var pId in partnerIds)
            {
                if (pId == null) continue;

                var lastMsg = await _context.TinNhan
                    .Where(t => ((t.NguoiGuiId == currentUserId && t.NguoiNhanId == pId) ||
                                 (t.NguoiGuiId == pId && t.NguoiNhanId == currentUserId)))
                    .OrderByDescending(t => t.ThoiGianGui)
                    .FirstOrDefaultAsync();

                var unreadCount = await _context.TinNhan
                    .CountAsync(t => t.NguoiGuiId == pId && t.NguoiNhanId == currentUserId && !t.DaDoc);

                var friend = await _context.NhanVien.FindAsync(pId);

                string previewMsg = lastMsg?.NoiDung;
                if (!string.IsNullOrEmpty(previewMsg) && previewMsg.StartsWith("[FILE]")) previewMsg = "📎 Đã gửi một tập tin";

                conversations.Add(new
                {
                    FriendId = pId,
                    FriendName = friend?.HoTen,
                    FriendAvatar = friend?.AnhDaiDien,
                    LastMessageContent = previewMsg,
                    LastMessageTime = lastMsg?.ThoiGianGui.AddHours(7).ToString("hh:mm tt"),
                    HasUnread = unreadCount > 0,
                    RawTime = lastMsg?.ThoiGianGui,
                    IsGroup = false
                });
            }

            // ==========================================
            // B. LẤY DANH SÁCH CÁC NHÓM MÀ NHÂN VIÊN ĐANG THAM GIA
            // ==========================================
            var myGroups = await _context.ThanhVienNhom
                .Where(tv => tv.MaNhanVien == currentUserId)
                .Include(tv => tv.NhomChat)
                .Select(tv => tv.NhomChat)
                .ToListAsync();

            foreach (var group in myGroups)
            {
                if (group == null) continue;

                // ĐÃ SỬA: Lấy tin nhắn cuối cùng + Tên người gửi từ bảng NhanVien
                var lastMsg = await _context.TinNhanNhom
                    .Include(t => t.NguoiGui)
                    .Where(t => t.MaNhom == group.MaNhom)
                    .OrderByDescending(t => t.ThoiGianGui)
                    .FirstOrDefaultAsync();

                string previewMsg = lastMsg?.NoiDung;
                if (!string.IsNullOrEmpty(previewMsg))
                {
                    if (previewMsg.StartsWith("[FILE]")) previewMsg = "📎 Đã gửi một tập tin";

                    // Ghép tên người gửi: Nếu là mình thì ghi "Bạn", người khác thì ghi Tên
                    string senderName = lastMsg.NguoiGuiId == currentUserId ? "Bạn" : lastMsg.NguoiGui?.HoTen;
                    previewMsg = $"{senderName}: {previewMsg}";
                }

                conversations.Add(new
                {
                    FriendId = group.MaNhom,
                    FriendName = group.TenNhom,
                    FriendAvatar = group.AnhNhom,
                    LastMessageContent = previewMsg ?? "Nhóm mới tạo, hãy gửi lời chào!",
                    LastMessageTime = lastMsg?.ThoiGianGui.AddHours(7).ToString("hh:mm tt") ?? group.NgayTao.AddHours(7).ToString("hh:mm tt"),
                    HasUnread = false,
                    RawTime = lastMsg?.ThoiGianGui ?? group.NgayTao,
                    IsGroup = true
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

        // 5. API UPLOAD FILE CHAT
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

                string fileExtension = Path.GetExtension(file.FileName);
                string uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";

                await supabase.Storage.From("chat-files").Upload(fileBytes, uniqueFileName);
                string fileUrl = supabase.Storage.From("chat-files").GetPublicUrl(uniqueFileName);

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

                return Ok(new { fileName = file.FileName, url = fileUrl });
            }
            catch (System.Exception ex)
            {
                string detailError = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return StatusCode(500, "Lỗi server: " + detailError);
            }
        }

        // 6. API TẠO NHÓM CHAT
        [HttpPost("create-group")]
        public async Task<IActionResult> CreateGroup([FromBody] CreateGroupRequest request)
        {
            if (string.IsNullOrEmpty(request.GroupName) || request.MemberIds == null || !request.MemberIds.Any())
            {
                return BadRequest("Tên nhóm và thành viên không được để trống.");
            }

            try
            {
                if (!request.MemberIds.Contains(request.CreatorId))
                {
                    request.MemberIds.Add(request.CreatorId);
                }

                var nhomChat = new NhomChat
                {
                    TenNhom = request.GroupName,
                    NguoiTaoId = request.CreatorId,
                    NgayTao = DateTime.UtcNow
                };

                _context.NhomChat.Add(nhomChat);
                await _context.SaveChangesAsync();

                foreach (var userId in request.MemberIds)
                {
                    _context.ThanhVienNhom.Add(new ThanhVienNhom
                    {
                        MaNhom = nhomChat.MaNhom,
                        MaNhanVien = userId
                    });
                }
                await _context.SaveChangesAsync();

                return Ok(new { success = true, maNhom = nhomChat.MaNhom, tenNhom = nhomChat.TenNhom });
            }
            catch (System.Exception ex)
            {
                string detail = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return StatusCode(500, "Lỗi server: " + detail);
            }
        }

        // 7. API LẤY LỊCH SỬ CHAT NHÓM
        [HttpGet("group-history/{groupId}")]
        public async Task<IActionResult> GetGroupChatHistory(int groupId)
        {
            var lichSu = await (from t in _context.TinNhanNhom
                                join nv in _context.NhanVien on t.NguoiGuiId equals nv.MaNhanVien
                                where t.MaNhom == groupId
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

        // =======================================================
        // 8. LẤY THÔNG TIN CHI TIẾT NHÓM
        // =======================================================
        [HttpGet("group/{groupId}/details")]
        public async Task<IActionResult> GetGroupDetails(int groupId)
        {
            var group = await _context.NhomChat.FindAsync(groupId);
            if (group == null) return NotFound("Không tìm thấy nhóm");

            var members = await _context.ThanhVienNhom
                .Where(tv => tv.MaNhom == groupId)
                .Include(tv => tv.NhanVien)
                .Select(tv => new {
                    tv.NhanVien.MaNhanVien,
                    tv.NhanVien.HoTen,
                    tv.NhanVien.AnhDaiDien
                }).ToListAsync();

            return Ok(new
            {
                group.MaNhom,
                group.TenNhom,
                group.AnhNhom,
                group.NguoiTaoId,
                members = members
            });
        }

        // =======================================================
        // 9. CẬP NHẬT ẢNH ĐẠI DIỆN NHÓM
        // =======================================================
        [HttpPost("group/{groupId}/avatar")]
        public async Task<IActionResult> UpdateGroupAvatar(int groupId, IFormFile file)
        {
            var group = await _context.NhomChat.FindAsync(groupId);
            if (group == null) return NotFound("Nhóm không tồn tại");
            if (file == null || file.Length == 0) return BadRequest("File không hợp lệ.");

            try
            {
                var options = new SupabaseOptions { AutoConnectRealtime = true };
                var supabase = new Supabase.Client(_supabaseUrl, _supabaseKey, options);
                await supabase.InitializeAsync();

                using var ms = new MemoryStream();
                await file.CopyToAsync(ms);
                var fileBytes = ms.ToArray();

                // Lưu ảnh vào chung bucket chat-files nhưng đặt tên tiền tố là avatar_group
                string fileExtension = Path.GetExtension(file.FileName);
                string uniqueFileName = $"avatar_group_{groupId}_{Guid.NewGuid()}{fileExtension}";

                await supabase.Storage.From("chat-files").Upload(fileBytes, uniqueFileName);
                string fileUrl = supabase.Storage.From("chat-files").GetPublicUrl(uniqueFileName);

                group.AnhNhom = fileUrl;
                await _context.SaveChangesAsync();

                return Ok(new { url = fileUrl });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Lỗi khi lưu ảnh: " + ex.Message);
            }
        }

        // =======================================================
        // 10. THÊM THÀNH VIÊN VÀO NHÓM
        // =======================================================
        [HttpPost("group/{groupId}/add-members")]
        public async Task<IActionResult> AddGroupMembers(int groupId, [FromBody] List<int> userIds)
        {
            var currentMembers = await _context.ThanhVienNhom.Where(tv => tv.MaNhom == groupId).Select(tv => tv.MaNhanVien).ToListAsync();
            foreach (var userId in userIds)
            {
                if (!currentMembers.Contains(userId))
                {
                    _context.ThanhVienNhom.Add(new ThanhVienNhom { MaNhom = groupId, MaNhanVien = userId });
                }
            }
            await _context.SaveChangesAsync();
            return Ok(new { success = true });
        }

        // =======================================================
        // 11. KÍCH THÀNH VIÊN / RỜI NHÓM
        // =======================================================
        [HttpDelete("group/{groupId}/member/{userId}")]
        public async Task<IActionResult> RemoveGroupMember(int groupId, int userId)
        {
            var member = await _context.ThanhVienNhom.FirstOrDefaultAsync(tv => tv.MaNhom == groupId && tv.MaNhanVien == userId);
            if (member != null)
            {
                _context.ThanhVienNhom.Remove(member);
                await _context.SaveChangesAsync();
            }
            return Ok(new { success = true });
        }

        // =======================================================
        // 12. XÓA NHÓM CHAT (CHỈ DÀNH CHO ADMIN)
        // =======================================================
        [HttpDelete("group/{groupId}")]
        public async Task<IActionResult> DeleteGroup(int groupId, [FromQuery] int requesterId)
        {
            var group = await _context.NhomChat.FindAsync(groupId);
            if (group == null) return NotFound("Nhóm không tồn tại");

            if (group.NguoiTaoId != requesterId) return BadRequest("Chỉ Trưởng nhóm mới có quyền xóa nhóm này.");

            // 1. Xóa các thành viên
            var members = _context.ThanhVienNhom.Where(tv => tv.MaNhom == groupId);
            _context.ThanhVienNhom.RemoveRange(members);

            // 2. Xóa các tin nhắn nhóm
            var messages = _context.TinNhanNhom.Where(t => t.MaNhom == groupId);
            _context.TinNhanNhom.RemoveRange(messages);

            // 3. Xóa nhóm
            _context.NhomChat.Remove(group);

            await _context.SaveChangesAsync();
            return Ok(new { success = true });
        }
        // =======================================================
        // 13. CẬP NHẬT TÊN NHÓM CHAT
        // =======================================================
        [HttpPut("group/{groupId}/name")]
        public async Task<IActionResult> UpdateGroupName(int groupId, [FromBody] string newName, [FromQuery] int requesterId)
        {
            var group = await _context.NhomChat.FindAsync(groupId);
            if (group == null) return NotFound("Nhóm không tồn tại");

            // Chỉ người tạo nhóm mới có quyền sửa tên
            if (group.NguoiTaoId != requesterId) return BadRequest("Chỉ Trưởng nhóm mới có quyền sửa tên nhóm.");

            if (string.IsNullOrWhiteSpace(newName)) return BadRequest("Tên nhóm không được để trống.");

            group.TenNhom = newName.Trim();
            await _context.SaveChangesAsync();

            return Ok(new { success = true, newName = group.TenNhom });
        }
    }
}