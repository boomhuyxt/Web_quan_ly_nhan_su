using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Web_quan_ly_nhan_su.Models
{
    [Table("LichCongTac")]
    public class LichCongTac
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int MaNhanVien { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ngày bắt đầu")]
        public DateTime NgayBatDau { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ngày kết thúc")]
        public DateTime NgayKetThuc { get; set; }

        [Required(ErrorMessage = "Địa điểm không được để trống")]
        [MaxLength(255)]
        public string DiaDiem { get; set; }

        [Required(ErrorMessage = "Nội dung công việc không được để trống")]
        public string NoiDungCongViec { get; set; }

        // Lưu đường link file (Word, PDF) đính kèm nội dung chi tiết công việc
        [MaxLength(500)]
        public string? FileDinhKemUrl { get; set; }

        // Trạng thái: "Sắp tới", "Đang diễn ra", "Đã hoàn thành", "Đã hủy"
        [MaxLength(50)]
        public string TrangThai { get; set; } = "Sắp tới";

        public DateTime NgayTao { get; set; } = DateTime.Now;

        // ==========================================
        // Khóa ngoại liên kết với bảng NhanVien
        // ==========================================
        [ForeignKey("MaNhanVien")]
        public virtual NhanVien? NhanVien { get; set; }
    }
}