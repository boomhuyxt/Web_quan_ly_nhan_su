using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;
using Web_quan_ly_nhan_su.Context;
using Web_quan_ly_nhan_su.Models;

namespace Web_quan_ly_nhan_su.Hubs
{
    // Kế thừa class Hub của SignalR
    public class ChatHub : Hub
    {
        private readonly AppDbContext _context;

        public ChatHub(AppDbContext context)
        {
            _context = context;
        }

        // Hàm này sẽ được Javascript (Client) gọi mỗi khi bấm nút Gửi
        public async Task SendMessage(int senderId, int receiverId, string message)
        {
            // 1. Lưu tin nhắn vào Database (PostgreSQL)
            var tinNhan = new TinNhan
            {
                NguoiGuiId = senderId,
                NguoiNhanId = receiverId,
                NoiDung = message,
                // Dùng UtcNow để không bị lỗi múi giờ với PostgreSQL
                ThoiGianGui = DateTime.UtcNow,
                DaDoc = false
            };

            _context.TinNhan.Add(tinNhan);
            await _context.SaveChangesAsync();

            // 2. Chuyển đổi giờ thành định dạng hh:mm tt để hiện lên web
            string timeString = tinNhan.ThoiGianGui.AddHours(7).ToString("hh:mm tt");

            // 3. Phát sóng (Broadcast) tin nhắn này tới TẤT CẢ các thiết bị đang mở web
            // (Javascript ở Client sẽ bắt sự kiện "ReceiveMessage" và vẽ tin nhắn ra màn hình)
            await Clients.All.SendAsync("ReceiveMessage", senderId, receiverId, message, timeString);
        }
    }
}