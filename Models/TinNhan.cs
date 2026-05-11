using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Web_quan_ly_nhan_su.Models
{
    [Table("TinNhan")]
    public class TinNhan
    {
        [Key]
        public int MaTinNhan { get; set; }

        [Required]
        public int NguoiGuiId { get; set; }

        // ĐÃ SỬA: Cho phép null (int?) vì chat nhóm sẽ không có NguoiNhan cụ thể
        public int? NguoiNhanId { get; set; }

        [Required]
        public string NoiDung { get; set; }

        public DateTime ThoiGianGui { get; set; } = DateTime.UtcNow;

        public bool DaDoc { get; set; } = false;

        public int? MaNhom { get; set; }

        // QUAN HỆ: Ai là người gửi
        [ForeignKey("NguoiGuiId")]
        public virtual NhanVien? NguoiGui { get; set; }

        // QUAN HỆ: Ai là người nhận (Dành cho chat 1-1)
        [ForeignKey("NguoiNhanId")]
        public virtual NhanVien? NguoiNhan { get; set; }

        // QUAN HỆ: Tin nhắn này thuộc nhóm nào (Dành cho chat Nhóm)
        [ForeignKey("MaNhom")]
        public virtual NhomChat? NhomChat { get; set; }
    }
}