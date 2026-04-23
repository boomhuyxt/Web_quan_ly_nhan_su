using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace Web_quan_ly_nhan_su.Models
{
        public class NhanVien
        {
            [Key]
            public int MaNhanVien { get; set; }

            [Required]
            public string HoTen { get; set; }

            [Required, EmailAddress]
            public string Email { get; set; } // Dùng làm tên đăng nhập

            [Required]
            public string MatKhauHash { get; set; } // Lưu mã băm SHA-256

            public string? SoDienThoai { get; set; }
            public string? GioiTinh { get; set; }
            public DateTime? NgaySinh { get; set; }
            public string? DiaChi { get; set; }
            public string? AnhDaiDien { get; set; }
            public DateTime? NgayVaoLam { get; set; }
            public int? TrangThai { get; set; }
            public int? MaPhongBan { get; set; }
            public string? FaceVector { get; set; } // Phục vụ FaceID
            public DateTime NgayTao { get; set; } = DateTime.Now;

            [ForeignKey("MaPhongBan")]
            public virtual PhongBan? PhongBan { get; set; }

            public virtual ICollection<NhanVienVaiTro> NhanVienVaiTro { get; set; }
        }
    
}
