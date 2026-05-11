using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
using System;

namespace Web_quan_ly_nhan_su.Models
{
    public class NhanVien
    {
        [Key]
        public int MaNhanVien { get; set; }

        [Required]
        public string HoTen { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }

        [Required]
        public string MatKhauHash { get; set; }

        public string? SoDienThoai { get; set; }
        public string? GioiTinh { get; set; }
        public DateTime? NgaySinh { get; set; }
        public string? DiaChi { get; set; }
        public string? AnhDaiDien { get; set; }
        public DateTime? NgayVaoLam { get; set; }
        public int? TrangThai { get; set; }
        public int? MaPhongBan { get; set; }
        public string? FaceVector { get; set; }
        public DateTime NgayTao { get; set; } = DateTime.Now;

        [ForeignKey("MaPhongBan")]
        public virtual PhongBan? PhongBan { get; set; }

        public virtual ICollection<NhanVienVaiTro> NhanVienVaiTro { get; set; }

        // --- CÁC QUAN HỆ MỚI CHO CHAT & NHÓM ---

        // 1 Nhân viên có thể tham gia nhiều Nhóm
        public virtual ICollection<ThanhVienNhom> NhomDaThamGia { get; set; } = new List<ThanhVienNhom>();

        // (InverseProperty giúp EF phân biệt đâu là tin đã gửi, đâu là tin đã nhận)
        [InverseProperty("NguoiGui")]
        public virtual ICollection<TinNhan> TinNhanDaGui { get; set; } = new List<TinNhan>();

        [InverseProperty("NguoiNhan")]
        public virtual ICollection<TinNhan> TinNhanDaNhan { get; set; } = new List<TinNhan>();
    }
}