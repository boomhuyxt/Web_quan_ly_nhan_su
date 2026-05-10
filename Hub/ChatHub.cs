using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;
using Web_quan_ly_nhan_su.Context;
using Web_quan_ly_nhan_su.Models;

namespace Web_quan_ly_nhan_su.Hubs
{
    public class ChatHub : Hub
    {
        private readonly AppDbContext _context;

        public ChatHub(AppDbContext context)
        {
            _context = context;
        }

        public async Task SendMessage(int nguoiGuiId, int nguoiNhanId, string noiDung)
        {
            // 1. Chuẩn hóa thời gian UTC để không bị lỗi PostgreSQL
            DateTime thoiGianGuiUtc = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);

            // Tính giờ VN để trả về luôn cho giao diện hiển thị
            string thoiGianHienThi = thoiGianGuiUtc.AddHours(7).ToString("hh:mm tt");

            // 2. Lưu tin nhắn vào Database
            var tinNhan = new TinNhan
            {
                NguoiGuiId = nguoiGuiId,
                NguoiNhanId = nguoiNhanId,
                NoiDung = noiDung,
                ThoiGianGui = thoiGianGuiUtc,
                DaDoc = false
            };

            _context.TinNhan.Add(tinNhan);
            await _context.SaveChangesAsync();

            // 3. Gửi tin nhắn đến các Client đang kết nối
            await Clients.All.SendAsync("ReceiveMessage", nguoiGuiId, nguoiNhanId, noiDung, thoiGianHienThi);
        }
    }
}