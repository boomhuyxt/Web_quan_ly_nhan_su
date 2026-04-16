using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Web_quan_ly_nhan_su.Models
{

        [Table("NghiPhep")]
        public class NghiPhep
        {
            [Key]
            public int Id { get; set; }

            [Required]
            public int NhanVienId { get; set; }

            [Required]
            public DateTime NgayBatDau { get; set; }

            [Required]
            public DateTime NgayKetThuc { get; set; }

            [MaxLength(100)]
            public string? LoaiNghi { get; set; } // Nghỉ phép, nghỉ ốm...

            public string? LyDo { get; set; }

            [MaxLength(50)]
            public string? TrangThai { get; set; } // Chờ duyệt, Đã duyệt, Từ chối

            [ForeignKey("NhanVienId")]
            public virtual NhanVien? NhanVien { get; set; }
        }
    
}
