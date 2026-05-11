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

        // 1. GỬI TIN NHẮN CÁ NHÂN (1-1)
        public async Task SendMessage(int senderId, int receiverId, string message)
        {
            // Lưu tin nhắn cá nhân vào bảng TinNhan
            var tinNhan = new TinNhan
            {
                NguoiGuiId = senderId,
                NguoiNhanId = receiverId,
                NoiDung = message,
                ThoiGianGui = DateTime.UtcNow,
                DaDoc = false
            };

            _context.TinNhan.Add(tinNhan);
            await _context.SaveChangesAsync();

            // Chuyển đổi giờ hiển thị (GMT+7)
            string timeString = tinNhan.ThoiGianGui.AddHours(7).ToString("hh:mm tt");

            // Phát sóng tới Client qua hàm "ReceiveMessage"
            await Clients.All.SendAsync("ReceiveMessage", senderId, receiverId, message, timeString);
        }

        // 2. GỬI TIN NHẮN NHÓM (Group Chat)
        public async Task SendGroupMessage(int senderId, int groupId, string message)
        {
            // ĐÃ SỬA: Sử dụng model TinNhanNhom mới bạn vừa tạo
            var tinNhan = new TinNhanNhom
            {
                NguoiGuiId = senderId,
                MaNhom = groupId,
                NoiDung = message,
                ThoiGianGui = DateTime.UtcNow
            };

            // Lưu vào bảng TinNhanNhom
            _context.TinNhanNhom.Add(tinNhan);
            await _context.SaveChangesAsync();

            // Lấy thông tin người gửi để hiển thị tên và ảnh trong khung chat nhóm
            var sender = await _context.NhanVien.FindAsync(senderId);
            string timeString = tinNhan.ThoiGianGui.AddHours(7).ToString("hh:mm tt");

            // Phát sóng tới Client qua hàm "ReceiveGroupMessage"
            // Kèm theo HoTen và AnhDaiDien của người gửi để hiện avatar ngay lập tức
            await Clients.All.SendAsync("ReceiveGroupMessage", senderId, groupId, message, timeString, sender?.HoTen, sender?.AnhDaiDien);
        }
    }
}