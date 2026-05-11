using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System;
using System.Collections.Generic;

namespace Web_quan_ly_nhan_su.Models
{
    public class NhomChat
    {
        [Key]
        public int MaNhom { get; set; }

        [Required]
        public string TenNhom { get; set; }

        public int NguoiTaoId { get; set; }

        public DateTime NgayTao { get; set; } = DateTime.UtcNow;

        public string? AnhNhom { get; set; }

        // QUAN HỆ: Trỏ tới bảng NhanVien để biết ai là người tạo
        [ForeignKey("NguoiTaoId")]
        public virtual NhanVien? NguoiTao { get; set; }

        // QUAN HỆ: 1 Nhóm có nhiều Thành viên
        public virtual ICollection<ThanhVienNhom> DanhSachThanhVien { get; set; } = new List<ThanhVienNhom>();

        // Đổi từ ICollection<TinNhan> sang ICollection<TinNhanNhom>
        public virtual ICollection<TinNhanNhom> DanhSachTinNhanNhom { get; set; } = new List<TinNhanNhom>();
    }
}