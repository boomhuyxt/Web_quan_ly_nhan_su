using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Web_quan_ly_nhan_su.Models
{
    [Table("TinNhanNhom")]
    public class TinNhanNhom
    {
        [Key]
        public int MaTinNhan { get; set; }

        [Required]
        public int MaNhom { get; set; } // Thuộc về nhóm nào

        [Required]
        public int NguoiGuiId { get; set; } // Ai là người gửi

        [Required]
        public string NoiDung { get; set; } // Nội dung tin nhắn (hoặc link file)

        public DateTime ThoiGianGui { get; set; } = DateTime.UtcNow;

        // QUAN HỆ: Liên kết với bảng NhomChat
        [ForeignKey("MaNhom")]
        public virtual NhomChat? NhomChat { get; set; }

        // QUAN HỆ: Liên kết với bảng NhanVien để biết tên/avatar người gửi
        [ForeignKey("NguoiGuiId")]
        public virtual NhanVien? NguoiGui { get; set; }
    }
}