using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Web_quan_ly_nhan_su.Models
{

        [Table("VaiTro")]
        public class VaiTro
        {
            [Key]
            public int MaVaiTro { get; set; }

            [MaxLength(100)]
            public string? MaCode { get; set; } // VD: ADMIN, USER, MANAGER

            [MaxLength(255)]
            public string? TenVaiTro { get; set; }

            // Quan hệ n-n với NhanVien thông qua bảng trung gian NhanVienVaiTro
            public virtual ICollection<NhanVienVaiTro>? NhanVienVaiTro { get; set; }
        }
    
}
