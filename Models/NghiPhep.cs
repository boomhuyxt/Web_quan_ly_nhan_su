using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http; // Bắt buộc thêm thư viện này để dùng IFormFile

namespace Web_quan_ly_nhan_su.Models
{
    [Table("NghiPhep")]
    public class NghiPhep
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int MaNhanVien { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ngày bắt đầu.")]
        public DateTime NgayBatDau { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ngày kết thúc.")]
        public DateTime NgayKetThuc { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn loại nghỉ.")]
        [MaxLength(100)]
        public string? LoaiNghi { get; set; } // Nghỉ phép, Nghỉ ốm, Nghỉ thai sản

        [Required(ErrorMessage = "Vui lòng nhập lý do.")]
        public string? LyDo { get; set; }

        [MaxLength(50)]
        public string? TrangThai { get; set; } // Chờ duyệt, Đã duyệt, Từ chối

        // Lưu URL trả về từ Supabase
        public string? MinhChungUrl { get; set; }

        [ForeignKey("MaNhanVien")]
        public virtual NhanVien? NhanVien { get; set; }

        // MỚI THÊM: Hứng file từ View. [NotMapped] giúp không báo lỗi khi lưu vào SQL Server
        [NotMapped]
        public IFormFile? FileMinhChung { get; set; }
    }
}